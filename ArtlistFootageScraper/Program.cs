using ArtlistFootageScraper.Services;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;
using static System.Globalization.CultureInfo;

namespace ArtlistFootageScraper
{
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Prompt:");
            string? title = Console.ReadLine();
            if (title == null) return;

            await GetRawSpeechFile(title);
            string[] temp = title.Split(' ');
            GetRawStockFootage(temp);
        }

        static async Task GetRawSpeechFile(string message)
        {
            // Setup logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<ITextToSpeechService>();

            //Getting File
            TextToSpeechService ttsService = new TextToSpeechService(logger);
            string? filePath = await ttsService.GetRawTTSFileAsync(message);
            if (filePath != null) OpenMediaFile(filePath);
        }

        static void GetRawStockFootage(string[] keywords)
        {
            var downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "stock-footage");

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            // Setup logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<StockFootageService>();

            //Chrome Options
            var options = SetupChromeOptions(downloadDirectory);

            // Setup WebDriver
            IWebDriver webDriver = new ChromeDriver(options);
            WebDriverWait webDriverWait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));

            // Setup File Service
            IFileService fileService = new FileService();

            new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
            webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // Initialize the service with its dependencies
            StockFootageService stockFootageService = new StockFootageService(webDriver, webDriverWait, logger, fileService);

            string? file = stockFootageService.GenerateStockFootageFromKeywordsSynchronously(keywords, downloadDirectory);
            OpenMediaFile(file);
        }

        static ChromeOptions SetupChromeOptions(string downloadDirectory)
        {
            var options = new ChromeOptions();

            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");
            options.AddUserProfilePreference("download.default_directory", downloadDirectory);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", true);
            options.AcceptInsecureCertificates = true;
            options.AddArgument("--headless");

            return options;
        }

        static void OpenMediaFile(string? filePath)
        {
            if (filePath == null)
            {
                Console.WriteLine($"Something went wrong while trying to open vide: {filePath}");
            }

            try
            {
                Process.Start("cmd", $"/c start \"\" \"{filePath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening the file: {ex.Message}");
            }
        }
    }
}