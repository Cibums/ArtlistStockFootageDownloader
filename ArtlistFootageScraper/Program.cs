using ArtlistFootageScraper.Services;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        private static string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "stock-footage");

        private static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            var processingService = host.Services.GetRequiredService<IVideoProcessingService>();
            host.Services.GetRequiredService<IVideoAnalysisService>();
            host.Services.GetRequiredService<IVideoUtilityService>();
            host.Services.GetRequiredService<IFileDetector>();
            host.Services.GetRequiredService<IStockFootageService>();
            host.Services.GetRequiredService<ITextToSpeechService>();

            if (File.Exists(outputDir + "\\video.mp4"))
            {
                File.Delete(outputDir + "\\video.mp4");
            }

            if (File.Exists(outputDir + "\\scenes.txt"))
            {
                File.Delete(outputDir + "\\scenes.txt");
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            ScriptResponse? script = JsonConvert.DeserializeObject<ScriptResponse>("{\r\n    \"title\": \"Bizarre Wonders of the Bat World\",\r\n    \"soundtrack_prompt\": \"Eerie, Enchanting Nature Documentary Instrumental\",\r\n    \"scenes\": [\r\n        {\r\n            \"message\": \"Enter the world of bats, where the bizarre meets the beautiful.\",\r\n            \"keywords\": [\"Bat\", \"Silhouette\", \"Night\"]\r\n        },\r\n        {\r\n            \"message\": \"Bats are the only mammals capable of sustained flight.\",\r\n            \"keywords\": [\"Flying\", \"Bat\", \"Sky\"]\r\n        },\r\n        {\r\n            \"message\": \"Some, like the vampire bat, survive solely on blood.\",\r\n            \"keywords\": [\"Vampire\", \"Bat\", \"Blood\"]\r\n        },\r\n        {\r\n            \"message\": \"With echolocation, they see the world through sound waves.\",\r\n            \"keywords\": [\"Echolocation\", \"Sound\", \"Waves\"]\r\n        },\r\n        {\r\n            \"message\": \"Bats play a crucial role in pollinating our favorite fruits.\",\r\n            \"keywords\": [\"Bat\", \"Flower\", \"Pollination\"]\r\n        },\r\n        {\r\n            \"message\": \"Their guano, or droppings, is a rich fertilizer for plants.\",\r\n            \"keywords\": [\"Guano\", \"Plants\", \"Fertilizer\"]\r\n        },\r\n        {\r\n            \"message\": \"From mystique to marvel, bats truly are nature's wonder.\",\r\n            \"keywords\": [\"Bat\", \"Nature\", \"Marvel\"]\r\n        }\r\n    ]\r\n}", settings);
            if (script == null) return;
            await RenderVideo(script, processingService);
        }

        static async Task RenderVideo(ScriptResponse? script, IVideoProcessingService processingService)
        {
            if (script == null || script.Scenes == null)
            {
                return;
            }

            foreach (ScriptScene scene in script.Scenes)
            {
                if (scene.Message == null || scene.Keywords == null) continue;

                string? ttsFilePath = await GetRawSpeechFile(scene.Message);

                string? footageFilePath = GetRawStockFootage(scene.Keywords);
                if (footageFilePath != null && ttsFilePath != null)
                {
                    string? scenePath = RenderScene(footageFilePath, ttsFilePath, processingService);
                    File.AppendAllText(outputDir + "\\scenes.txt", "file '" + scenePath + "'" + Environment.NewLine);
                }
            }

            processingService.ConcatenateVideos(outputDir + "\\scenes.txt", outputDir + "\\video.mp4");
            File.Delete(outputDir + "\\scenes.txt");
            OpenMediaFile(outputDir + "\\video.mp4");

            //Add Music
        }

        static string? RenderScene(string footagePath, string speechPath, IVideoProcessingService processingService)
        {
            string? filePath = processingService.RenderFootage(footagePath, speechPath);
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Register services
                services.AddSingleton<IVideoAnalysisService, VideoAnalysisService>();
                services.AddSingleton<IVideoUtilityService, VideoUtilityService>();
                services.AddSingleton<IVideoProcessingService, VideoProcessingService>();
            });
    }
}