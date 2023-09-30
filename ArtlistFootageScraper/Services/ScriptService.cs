using AngleSharp.Scripting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class ScriptService : IScriptService
    {
        private readonly IChatGPTService _chatGPTService;
        private readonly ILogger<IScriptService> _logger;

        public ScriptService(IChatGPTService chatGPTService, ILogger<IScriptService> logger)
        {
            _chatGPTService = chatGPTService ?? throw new ArgumentNullException(nameof(chatGPTService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ScriptResponse?> GetScript(string keyword, int videoLength)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentException("Keyword cannot be null or whitespace.", nameof(keyword));
            }

            _logger.LogInformation($"Getting Script for keyword: {keyword}");

            string prompt = GeneratePrompt(keyword, videoLength);
            GPTResponse? gptResponse = await _chatGPTService.CallChatGPT(prompt);
            string? message = gptResponse?.Choices[0].Message.Content;

            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Received empty response from ChatGPT");
                return null;
            }

            ScriptResponse response = DeserializeScriptResponse(message);
            if (response == null) return null;

            return response;
        }

        private string GeneratePrompt(string keyword, int videoLength)
        {
            return "Give me a script for a " + videoLength.ToString() + "-second short educational video about the " + keyword + ". The script should be in the style of a David Attenborough documentary. Focus on unusual and weird facts to leave the audience amazed. The video should be a list of scenes. The result will be a video where a narrator is speaking over stock footage.    \r\n\r\nYour response should be in the form of a json response in a codeblock. This is the structure of the json:\r\n\r\nThe title and the message in the first scene should be some hook, to make the viewer want to know more.\r\n\r\nThe soundtrack_prompt should be a prompt for an AI to find a good background soundtrack for the video. \r\n\r\nThe keywords will be used to find stock footage, so think about what you want the background image to be together with the audio. Also make sure that the keywords fit the theme of the whole video, not only this scene. Use a maximum of 3 keywords per scene. Only do single words. Do for example not use the word \"Disco Ball\", because that contains a space. Use less than three if you want to be more specific about what you want to show in the background footage. You are encouraged to be as specific as possible and use the least amount of keywords as possible. The keywords should be nouns, and not anything else. No adjectives or adverbs. Only nouns.         \r\n\r\n{\r\n    title: \"Amazing Facts About Ancient Egypt\"\r\n    soundtrack_prompt: \"Mysterious Instrumental Background\"\r\n    scenes: [\r\n        {\r\n            message: \"Amazing Facts About Ancient Egypt\",\r\n            keywords: [\"Egypt\"],\r\n        },\r\n        {\r\n            message: \"Egypt is the home to marvelous pyramids\",\r\n            keywords: [\"Pyramids\"],\r\n        },\r\n    ]\r\n} ";
        }

        private ScriptResponse? DeserializeScriptResponse(string message)
        {
            var regex = new Regex(@"```(.*?)```", RegexOptions.Singleline);
            var match = regex.Match(message);

            string contentToDeserialize = match.Success ? match.Groups[1].Value : message;

            try
            {
                return JsonConvert.DeserializeObject<ScriptResponse>(contentToDeserialize);
            }
            catch (JsonException ex) // Catch specific exception
            {
                _logger.LogError(ex, "Unable to deserialize script response");
                throw;
            }
        }
    }
}
