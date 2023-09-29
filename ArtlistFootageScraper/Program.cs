using ArtlistFootageScraper.Services;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            ScriptResponse? script = JsonConvert.DeserializeObject<ScriptResponse>("{\r\n    \"title\": \"Amazing Facts About Crows\",\r\n    \"soundtrack_prompt\": \"Mystical Nature Instrumental Background\",\r\n    \"scenes\": [\r\n        {\r\n            \"message\": \"Amazing Facts About Crows\",\r\n            \"keywords\": [\"Crows\", \"Flying\", \"Sky\"]\r\n        },\r\n        {\r\n            \"message\": \"Crows are among the most intelligent birds on Earth.\",\r\n            \"keywords\": [\"Crows\", \"Brain\", \"Intelligence\"]\r\n        },\r\n        {\r\n            \"message\": \"They can recognize human faces and remember them for years.\",\r\n            \"keywords\": [\"Crows\", \"Human Faces\", \"Memory\"]\r\n        }\r\n    ]\r\n}", settings);
            if (script == null) return;
            await RenderVideo(script);
        }

        static async Task RenderVideo(ScriptResponse? script)
        {
            if (script == null || script.Scenes == null)
            {
                return;
            }

            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "stock-footage");

            foreach (ScriptScene scene in script.Scenes)
            {
                if (scene.Message == null || scene.Keywords == null) continue;

                string? ttsFilePath = await GetRawSpeechFile(scene.Message);

                string? footageFilePath = GetRawStockFootage(scene.Keywords);
                if (footageFilePath != null && ttsFilePath != null)
                {
                    string? scenePath = RenderScene(footageFilePath, ttsFilePath);
                    File.AppendAllText(outputDir + "\\scenes.txt", "file '" + scenePath + "'" + Environment.NewLine);
                }
            }

            VideoAnalyzerService service = new VideoAnalyzerService();
            service.ConcatenateVideos(outputDir + "\\scenes.txt", outputDir + "\\video.mp4");
            File.Delete(outputDir + "\\scenes.txt");
            OpenMediaFile(outputDir + "\\video.mp4");

            //Add Music
        }

        static string? RenderScene(string footagePath, string speechPath)
        {
            VideoAnalyzerService service = new VideoAnalyzerService();
            string? filePath = service.RenderFootage(footagePath, speechPath);
            return filePath;
        }

        static async Task<string?> GetRawSpeechFile(string message)
        {
            // Setup logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<ITextToSpeechService>();

            //Getting File
            TextToSpeechService ttsService = new TextToSpeechService(logger);
            return await ttsService.GetRawTTSFileAsync(message);
        }

        static string? GetRawStockFootage(string[] keywords)
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

            return stockFootageService.GenerateStockFootageFromKeywordsSynchronously(keywords, downloadDirectory);
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