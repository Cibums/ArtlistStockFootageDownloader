using ArtlistFootageScraper.Exceptions;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools.V115.Page;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly string downloadDirectory;
        private readonly HttpClient httpClient;
        private const string BaseUri = "https://large-text-to-speech.p.rapidapi.com";

        private readonly ILogger<ITextToSpeechService> _logger;

        public TextToSpeechService(ILogger<ITextToSpeechService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "speech");
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUri)
            };

            httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", AppConfiguration.RAPID_API_KEY);
            httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", BaseUri);
        }

        public async Task<string> DownloadAudioFileAsync(string downloadUrl, string targetFolderPath)
        {
            _logger.LogInformation($"Downloading audio file from {downloadUrl}");

            using var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var fileName = GetFileNameFromUrl(downloadUrl) ?? throw new InvalidOperationException("Failed to get the file name from the URL");
            var targetFilePath = Path.Combine(targetFolderPath, fileName);

            using var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);

            _logger.LogInformation($"Audio file downloaded successfully to {targetFilePath}");

            return targetFilePath;
        }

        private static string? GetFileNameFromUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ? Path.GetFileName(uri.LocalPath) : null;
        }

        public async Task<string?> GetRawTTSFileAsync(string message)
        {
            _logger.LogInformation("Getting raw TTS file");

            if (!Directory.Exists(downloadDirectory))
            {
                _logger.LogInformation("Creating download directory");
                Directory.CreateDirectory(downloadDirectory);
            }

            var filePath = SpeechFileExists(message);
            if (!string.IsNullOrEmpty(filePath)) return filePath;

            var createdJob = await CreateSpeechFileAsync(message);
            if (!createdJob.HasValue) return null;

            await Task.Delay(5000);
            var downloadPath = await GetDownloadPathForTTSFileAsync(createdJob.Value);
            if (string.IsNullOrEmpty(downloadPath)) return null;

            filePath = await DownloadAudioFileAsync(downloadPath, downloadDirectory);

            _logger.LogInformation("Saving file to storage");
            var storage = StorageManager.LoadStorage();
            storage.SpeechLinks[message] = filePath;
            StorageManager.SaveStorage(storage);

            if (!string.IsNullOrEmpty(filePath))
                _logger.LogInformation($"File path found: {filePath}");
            else
                _logger.LogWarning("File path not found");

            return filePath;
        }

        private string SpeechFileExists(string message)
        {
            var storage = StorageManager.LoadStorage();
            if (storage == null) return string.Empty;
            return storage.SpeechLinks.TryGetValue(message, out var filePath) && File.Exists(filePath) ? filePath : string.Empty;
        }

        public async Task<Guid?> CreateSpeechFileAsync(string message)
        {
            _logger.LogInformation($"Creating Speech File for message: {message}");

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://large-text-to-speech.p.rapidapi.com/tts"),
                Headers =
                {
                    { "X-RapidAPI-Key", AppConfiguration.RAPID_API_KEY },
                    { "X-RapidAPI-Host", "large-text-to-speech.p.rapidapi.com" },
                },
                Content = new StringContent("{\r\n    \"text\": \"" + message + "\"\r\n}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TtsJobResponseModel>(body);
            return result?.Id;
        }

        public async Task<string?> GetDownloadPathForTTSFileAsync(Guid guid)
        {
            _logger.LogInformation($"Getting download path for TTS file with GUID {guid}");

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://large-text-to-speech.p.rapidapi.com/tts?id={guid}"),
                Headers =
                {
                    { "X-RapidAPI-Key", AppConfiguration.RAPID_API_KEY },
                    { "X-RapidAPI-Host", "large-text-to-speech.p.rapidapi.com" },
                },
            };

            const int maxRetries = 10;
            int currentRetry = 0;
            int milliSecondsDelay = 1000;

            while (currentRetry < maxRetries)
            {
                try
                {
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<TtsResponseModel>(body);

                        if (result?.Status == "success")
                        {
                            return result?.Url;
                        }
                    }
                }
                catch
                {
                    milliSecondsDelay += 1000;
                }

                currentRetry++;
                await Task.Delay(milliSecondsDelay);
            }

            _logger.LogError("Failed to get successful status after max retries.");
            throw new MaxRetriesException("Failed to get successful status after max retries.");
        }
    }
}
