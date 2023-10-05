using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Chrome;
using WebDriverManager.DriverConfigs.Impl;
using static System.Globalization.CultureInfo;

namespace ArtlistFootageScraper.Services
{
    public class MusicService : IMusicService
    {
        private readonly string PAGE_URL = @"https://incompetech.com/music/royalty-free/music.html";
        private readonly string downloadDirectory = AppConfiguration.musicOutputPath;
        private readonly IWebDriver _driver;
        private WebDriverWait _wait;
        private readonly ILogger<IMusicService> _logger;
        private readonly IFileService _fileService;

        public MusicService(ILogger<IMusicService> logger, IFileService fileService)
        {
            _driver = CreateDriver();
            _logger = logger;
            _fileService = fileService;
        }

        private IWebDriver CreateDriver()
        {
            //Chrome Options
            var options = Program.SetupChromeOptions(downloadDirectory);

            // Setup WebDriver
            IWebDriver webDriver = new ChromeDriver(options);
            _wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));

            new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
            webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            return webDriver;
        }

        public string DownloadMusic(string feels, string tempo, string genre)
        {
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            var storageKey = feels + tempo + genre;
            var storage = StorageManager.LoadStorage();

            string alreadyExistingMusic = MusicFileExists(storageKey, storage);
            if (!string.IsNullOrEmpty(alreadyExistingMusic))
            {
                _logger.LogInformation("Getting Already Downloaded Music");
                return alreadyExistingMusic;
            }

            _logger.LogInformation("Searching for music");
            //Opens page
            _driver.Navigate().GoToUrl(PAGE_URL);

            SelectOption("Feels", feels);
            SelectOption("Tempo", tempo);
            SelectOption("Genre", genre);
            SelectFirstTableRow();
            _logger.LogInformation("Downloading Music");
            Download();
            _fileService.WaitForDownloadStart(downloadDirectory);
            _fileService.WaitForDownloadCompletion(downloadDirectory);
            string fileName = _fileService.GetLatestChangedFile(downloadDirectory);

            string newFileName = _fileService.RenameFileToSnakeCase(fileName);

            _logger.LogInformation("Saving Music File In Storage");
            storage.MusicLinks[storageKey] = newFileName;
            StorageManager.SaveStorage(storage);

            return newFileName;
        }

        private string MusicFileExists(string message, Storage storage)
        {
            if (storage == null) return string.Empty;
            return storage.MusicLinks.TryGetValue(message, out var filePath) && File.Exists(filePath) ? filePath : string.Empty;
        }

        private void SelectOption(string buttonText, string optionText)
        {
            string optionButtonText = CurrentCulture.TextInfo.ToTitleCase(buttonText.ToLower().Trim());

            IWebElement? feelsButton = _wait.Until(driver =>
            {
                var elements = driver.FindElements(By.XPath($"//button[contains(@class, 'btn-xs btn-default dropdown-toggle') and @data-toggle='dropdown' and contains(text(), '{optionButtonText}')]"));
                return elements.Count > 0 && elements[0].Displayed && elements[0].Enabled ? elements[0] : null;
            });
            feelsButton?.Click();

            string optionAlternativeButtonText = CurrentCulture.TextInfo.ToTitleCase(optionText.ToLower().Trim());

            IWebElement? feelsCheckbox = _wait.Until(driver =>
            {
                var elements = driver.FindElements(By.XPath($"//a[contains(text(),'{optionAlternativeButtonText}')]"));
                return elements.Count > 0 && elements[0].Displayed && elements[0].Enabled ? elements[0] : null;
            });
            feelsCheckbox?.Click();
        }

        private void SelectFirstTableRow(int increase = 0)
        {
            IWebElement? firstTableRow = _wait.Until(driver =>
            {
                var elements = driver.FindElements(By.XPath("//tbody[@class='filtered-song-list-body']/tr[@class='search-result-row'][1]"));
                return elements.Count > 0 && elements[0 + increase].Displayed && elements[0 + increase].Enabled ? elements[0 + increase] : null;
            });

            string timeLength = "";
            if (firstTableRow != null)
            {
                var tdElements = firstTableRow.FindElements(By.TagName("td"));
                if (tdElements.Count > 0)
                {
                    timeLength = tdElements[tdElements.Count - 1].Text;
                }
            }

            if (string.IsNullOrEmpty(timeLength))
            {
                firstTableRow?.Click();
                return;
            }

            int seconds = 0;

            try
            {
                seconds = ConvertToSeconds(timeLength);
            }
            catch
            {
                firstTableRow?.Click();
                return;
            }

            if (seconds < 90)
            {
                SelectFirstTableRow(1 + increase);
                return;
            }

            firstTableRow?.Click();
            return;
        }

        private int ConvertToSeconds(string timeString)
        {
            string[] timeParts = timeString.Split(':');
            if (timeParts.Length != 2)
            {
                throw new FormatException("Invalid time format. Expected format: mm:ss");
            }

            int minutes;
            int seconds;
            if (!int.TryParse(timeParts[0], out minutes) || !int.TryParse(timeParts[1], out seconds))
            {
                throw new FormatException("Invalid time format. Expected numeric values for minutes and seconds.");
            }

            return minutes * 60 + seconds;
        }

        private void Download()
        {
            IWebElement? downloadLink = _wait.Until(driver =>
            {
                var elements = driver.FindElements(By.XPath("//a[contains(@class, 'btn btn-primary btn-sm') and contains(text(), 'Download')]"));
                return elements.Count > 0 && elements[0].Displayed && elements[0].Enabled ? elements[0] : null;
            });
            downloadLink?.Click();
        }
    }
}
