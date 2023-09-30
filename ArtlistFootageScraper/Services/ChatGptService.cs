using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class ChatGptService : IChatGPTService
    {
        private readonly string OPENAI_API_KEY = AppConfiguration.OPENAI_API_KEY;
        private readonly string OPENAI_API_URL = @"https://api.openai.com/v1/chat/completions";

        private readonly ILogger<ChatGptService> _logger;

        public ChatGptService(ILogger<ChatGptService> logger)
        {
            _logger = logger;
        }

        public async Task<GPTResponse?> CallChatGPT(string prompt)
        {
            _logger.LogInformation("Calling ChatGPT");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OPENAI_API_KEY}");

                var payload = new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(OPENAI_API_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully got GPT response");
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    return JsonConvert.DeserializeObject<GPTResponse>(responseBody, settings);
                }
                else
                {
                    _logger.LogError($"An error occured with status code {response.StatusCode}");
                    throw new Exception($"An error occured with status code {response.StatusCode}");
                }
            }
        }
    }
}
