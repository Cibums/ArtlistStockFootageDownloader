using DotNetEnv;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using WebDriverManager.DriverConfigs.Impl;
using static System.Globalization.CultureInfo;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ArtlistFootageScraper
{
    public class StockFootageService : IStockFootageService
    {
        private const int LOGIN_TIMEOUT = 10;
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly ILogger<IStockFootageService> _logger;
        private readonly IFileService _fileService;

        public StockFootageService(IWebDriver driver, WebDriverWait wait, ILogger<IStockFootageService> logger, IFileService fileService)
        {
            _driver = driver;
            _wait = wait;
            _logger = logger;
            _fileService = fileService;
        }

        public string? GenerateStockFootageFromKeywordsSynchronously(string[] keywords, string downloadDirectory)
        {
            try
            {
                Login();
                SearchFootage(keywords);
                DownloadFootage(downloadDirectory);
                return _fileService?.GetLatestDownloadedFile(downloadDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
                throw new StockFootageException("Failed to generate stock footage.", ex);
            }
            finally
            {
                _driver.Quit();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "This will never be anything else and there's no reason for this to not be hardcoded")]
        private void Login()
        {
            // Navigate to the login page
            _driver.Navigate().GoToUrl("https://artlist.io/");

            // Wait and click the sign-in button
            IWebElement? signInButton = _wait.Until(driver =>
            {
                var elements = driver.FindElements(By.XPath("//div[contains(text(), 'Sign in')]"));
                return (elements.Count > 0 && elements[0].Displayed && elements[0].Enabled) ? elements[0] : null;
            });
            signInButton?.Click();

            // Load environment variables for login details
            string? parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
            if (parentDirectory == null)
            {
                throw new DirectoryNotFoundException("Parent directory not found. Cannot proceed without a valid directory.");
            }

            string envFilePath = Path.Combine(parentDirectory, ".env");
            Env.Load(envFilePath);

            string EMAIL = Env.GetString("EMAIL");
            string PASSWORD = Env.GetString("PASSWORD");
            string FIRST_NAME = Env.GetString("FIRST_NAME");
            string firstname = CurrentCulture.TextInfo.ToTitleCase(FIRST_NAME.ToLower().Trim());

            // Input email and password
            IWebElement emailInput = _wait.Until<IWebElement>(d => d.FindElement(By.CssSelector("input[data-testid='email']")));
            emailInput.SendKeys(EMAIL);

            IWebElement? passwordInput = _wait.Until(d =>
            {
                var element = d.FindElement(By.CssSelector("input[data-testid='password'][aria-label='password']"));
                return element.Displayed && element.Enabled ? element : null;
            });
            passwordInput?.Click();
            passwordInput?.SendKeys(PASSWORD);

            // Click login button
            var loginButton = _wait.Until(driver => driver.FindElement(By.XPath("//button[contains(., 'Sign in')]")));
            loginButton.Click();

            // Wait to confirm successful login
            bool isLoggedIn = false;
            DateTime timeout = DateTime.Now.AddSeconds(LOGIN_TIMEOUT);
            while (DateTime.Now < timeout)
            {
                try
                {
                    IWebElement loggedInElement = _driver.FindElement(By.XPath($"//div[text()='{firstname}']"));
                    if (loggedInElement.Displayed)
                    {
                        isLoggedIn = true;
                        break;
                    }
                }
                catch (NoSuchElementException)
                {
                    System.Threading.Thread.Sleep(500);  // Wait briefly before checking again
                }
            }

            if (!isLoggedIn)
            {
                throw new InvalidOperationException("Failed to detect the logged-in state within the timeout period.");
            }
        }

        private void SearchFootage(string[] keywords)
        {
            // Navigate to the search footage page with the specified keywords
            _driver.Navigate().GoToUrl($"https://artlist.io/stock-footage/search?terms={string.Join(", ", keywords)}");

            // Wait for the search results to load
            _wait.Until(d => d.FindElements(By.XPath("//div[@data-id='finder-content']")).Count > 0);
        }

        private void DownloadFootage(string downloadDirectory)
        {
            // Navigate to the first footage result
            var footageView = _driver.FindElement(By.XPath("//div[@data-id='finder-content']"));
            var videoItemContainer = _wait.Until(driver =>
            {
                var element = footageView.FindElement(By.XPath(".//div[@data-testid='video-item-container']"));
                return (element != null && element.Displayed) ? element : null;
            });

            string? href = videoItemContainer?.FindElement(By.XPath(".//a")).GetAttribute("href");
            _driver.Navigate().GoToUrl(href);

            // Initiate download
            IWebElement? downloadButton = null;

            downloadButton = _wait.Until(d =>
            {
                var elements = d.FindElements(By.XPath("//button[@aria-label='Download']"));
                var checkButton = elements.Any() ? null : d.FindElement(By.XPath("//button[@type='button' and @class='flex justify-center items-center font-bold rounded-full disabled:cursor-not-allowed disabled:text-primary disabled:bg-gray-300 disabled:border-gray-300 transition-smooth disabled:text-gray-300 bg-transparent disabled:bg-transparent rounded-full border border-accent text-accent text-accent-100 hover:border-accent-100 w-9 h-9 grid place-items-center text-sm leading-4']"));

                return elements.Any(e => e.Displayed && e.Enabled) ? elements.First() : checkButton;
            });

            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].click();", downloadButton);

            var hdPreviewButton = _wait.Until(d =>
            {
                var elements = d.FindElements(By.XPath("//button[.//span[text()='HD Preview']]"));
                return elements.Any(e => e.Displayed && e.Enabled) ? elements.First() : null;
            });

            hdPreviewButton?.Click();

            // Wait for the download to start
            WaitForDownloadStart(downloadDirectory);
            _logger.LogInformation("Download Started");

            WaitForDownloadCompletion(downloadDirectory);
            _logger.LogInformation("Download Completed");
        }

        public static void WaitForDownloadStart(string directory)
        {
            var end = DateTime.Now.AddMinutes(1);  // 1 minute timeout
            while (DateTime.Now < end)
            {
                if (Directory.GetFiles(directory, "*.crdownload").Any())
                    return;
                System.Threading.Thread.Sleep(500); // Check every half-second
            }
            throw new TimeoutException("Download did not start within expected time.");
        }

        public static void WaitForDownloadCompletion(string directory)
        {
            var end = DateTime.Now.AddMinutes(5);  // 5 minutes timeout, adjust as needed
            while (DateTime.Now < end)
            {
                if (!Directory.GetFiles(directory, "*.crdownload").Any())
                    return;
                System.Threading.Thread.Sleep(1000); // Check every second
            }
            throw new TimeoutException("Download did not complete within expected time.");
        }
    }
}
