using AngleSharp.Html.Dom;
using ArtlistFootageScraper.Services;
using DotNetEnv;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Text.RegularExpressions;
using WebDriverManager.DriverConfigs.Impl;
using static System.Globalization.CultureInfo;

namespace ArtlistFootageScraper
{
    public static class Program
    {
        public static string VideoTitle = "render";
        private static string AnimalsList = Path.GetFullPath(@"..\..\..\animals.txt");
        private const string VideoFileName = "video.mp4";
        private const string ScenesFileName = "scenes.txt";
        private static readonly string OutputDir = AppConfiguration.stockFootageOutputPath;

        private static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            await ExecuteVideoCreationAsync(host.Services);
            Environment.Exit(0);
        }

        private static async Task ExecuteVideoCreationAsync(IServiceProvider services)
        {
            var fileService = services.GetRequiredService<IFileService>();
            DeleteExistingFiles(fileService);

            var script = await GetScript(services);
            if (script == null) return;
            if (script.Soundtrack_Prompt == null) return;

            string render = await RenderVideo(script, services);

            OpenMediaFile(render);
        }

        private static void DeleteExistingFiles(IFileService fileService)
        {
            fileService.DeleteIfExists(Path.Combine(OutputDir, VideoFileName));
            fileService.DeleteIfExists(Path.Combine(OutputDir, ScenesFileName));
            fileService.DeleteVideoTempFiles(OutputDir);
        }

        private static async Task<ScriptResponse?> GetScript(IServiceProvider services)
        {
            string[] animals = File.ReadAllLines(AnimalsList);
            Random random = new Random();
            int randomIndex = random.Next(animals.Length);
            string scriptWord = animals[randomIndex];

            var scriptService = services.GetRequiredService<IScriptService>();
            ScriptResponse? response = await scriptService.GetScript(scriptWord, 10);
            if (response == null) throw new ArgumentNullException(nameof(response));
            VideoTitle = response.Title;
            response.AddGeneralKeyword(scriptWord);
            return response;
        }

        private static async Task<string> RenderVideo(ScriptResponse script, IServiceProvider services)
        {
            var processingService = services.GetRequiredService<IVideoProcessingService>();
            var ttsService = services.GetRequiredService<ITextToSpeechService>();
            var fileService = services.GetRequiredService<IFileService>();

            foreach (var scene in script.Scenes)
            {
                if (scene.Message == null || scene.Keywords == null) continue;

                var ttsFilePath = await ttsService.GetRawTTSFileAsync(scene.Message);
                var footageFilePath = GetRawStockFootage(scene.Keywords.ToArray(), fileService);

                if (footageFilePath != null && ttsFilePath != null)
                {
                    var scenePath = RenderScene(footageFilePath, ttsFilePath, processingService);
                    fileService.AppendAllText(Path.Combine(OutputDir, ScenesFileName), $"file '{scenePath}'{Environment.NewLine}");
                }
            }

            processingService.ConcatenateVideos(Path.Combine(OutputDir, ScenesFileName), Path.Combine(OutputDir, VideoFileName));

            if (!Directory.Exists(AppConfiguration.renderingsOutputPath)) Directory.CreateDirectory(AppConfiguration.renderingsOutputPath);

            //Getting video length
            VideoCapture videoCapture = new VideoCapture(Path.Combine(OutputDir, VideoFileName));
            double totalFrames = videoCapture.Get(CapProp.FrameCount);
            double fps = videoCapture.Get(CapProp.Fps);
            double videoDurationInSeconds = totalFrames / fps;

            var musicService = services.GetRequiredService<IMusicService>();
            string musicFilePath = musicService.DownloadMusic(script.Soundtrack_Prompt[0], script.Soundtrack_Prompt[1], script.Soundtrack_Prompt[2]);

            string normalizedMusicFilePath = processingService.NormalizeAudioVolume(musicFilePath);
            string trimmedMusicFilePath = processingService.CutAudioAndAdjustVolume(normalizedMusicFilePath, (float)videoDurationInSeconds);

            string render = processingService.AddMusicToVideo(Path.Combine(OutputDir, VideoFileName), trimmedMusicFilePath, AppConfiguration.renderingsOutputPath + "\\" + fileService.ConvertToSnakeCase(script.Title) + ".mp4");

            fileService.DeleteIfExists(Path.Combine(OutputDir, ScenesFileName));

            return render;
        }



        static string? RenderScene(string footagePath, string speechPath, IVideoProcessingService processingService)
        {
            string? filePath = processingService.RenderFootage(footagePath, speechPath);
            return filePath;
        }

        static string? GetRawStockFootage(string[] keywords, IFileService fileService)
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

            new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
            webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // Initialize the service with its dependencies
            StockFootageService stockFootageService = new StockFootageService(webDriver, webDriverWait, logger, fileService);

            return stockFootageService.GenerateStockFootageFromKeywordsSynchronously(keywords, downloadDirectory);
        }

        public static ChromeOptions SetupChromeOptions(string downloadDirectory)
        {
            var options = new ChromeOptions();

            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");
            options.AddUserProfilePreference("download.default_directory", downloadDirectory);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", true);
            options.AcceptInsecureCertificates = true;
            //options.AddArgument("--headless");

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
                services.AddSingleton<ITextToSpeechService, TextToSpeechService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IChatGPTService, ChatGptService>();
                services.AddSingleton<IScriptService, ScriptService>();
                services.AddSingleton<IMusicService, MusicService>();
            });
    }
}