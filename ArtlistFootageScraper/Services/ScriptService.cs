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
            return "Give me a script for a " + videoLength.ToString() + "-second short educational video about the " + keyword + ". The script should be written in an unusal and funny way, using internet slang. Focus on unusual and weird facts to leave the audience amazed. Do not use any emojis in the script. The video should be a list of scenes. The result will be a video where a narrator is speaking over stock footage.    \r\n\r\nYour response should be in the form of a json response in a codeblock. Only the code block and nothing else should be in your response. This is the structure of the json:\r\n\r\nThe title and the message in the first scene should be some hook, to make the viewer want to know more. The last scene's message should be something conclusive.\r\n\r\nThe soundtrack_prompt should be a string[] of keywords. \r\n\r\nKeyword one should be the \"feels\". Choose between these words: Action, Aggressive, Bouncy, Bright, Calm, Calming, Dark, Driving, Eerie, Epic, Grooving, Intense, Mysterious, Mystical, Relaxed, Somber, Suspenseful, Unnerving, Uplifting\r\n\r\nKeyword two should be the desired BPM. Your options here are: \"< 62\", \"62-74\", \"74-90\", \"90-120\", \"121-150\", \"> 150\"\r\n\r\nKeyword three should be genre. Choose between these words: African, Blues, Classical, Contemporary, Disco, Electronica, Funk, Holiday, Horror, Jazz, Latin, Modern, Musical, Polka, Pop, Reggae, Rock, Silent Film Score, Ska, Soundtrack, Stings, Unclassifiable, World, Urban\r\n\r\nThe keywords will be used to find stock footage, so think about what you want the background image to be together with the audio. Also make sure that the keywords fit the theme of the whole video, not only this scene. Use a maximum of 3 keywords per scene. Only do single words. Do for example not use the word \"Disco Ball\", because that contains a space. Use less than three if you want to be more specific about what you want to show in the background footage. The keywords should be nouns, and not anything else. No adjectives or adverbs. Only nouns.         \r\n\r\n{\r\n    title: \"Amazing Facts About Ancient Egypt\"\r\n    soundtrack_prompt: [\"Mysterious\", \"90-120\", \"World\"]\r\n    scenes: [\r\n        {\r\n            message: \"Amazing Facts About Ancient Egypt\",\r\n            keywords: [\"Egypt\"],\r\n        },\r\n        {\r\n            message: \"Egypt is the home to marvelous pyramids\",\r\n            keywords: [\"Pyramids\"],\r\n        },\r\n    ]\r\n} ";
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
