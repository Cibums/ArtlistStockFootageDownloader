using AngleSharp.Common;
using Newtonsoft.Json;
using OpenQA.Selenium.Internal;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper
{
    public class ScriptResponse
    {
        public string? Title { get; set; }
        public string[]? Soundtrack_Prompt { get; set; }

        public ScriptScene[]? Scenes { get; set; }

        public void AddGeneralKeyword(string word)
        {
            if (Scenes == null) return;

            foreach (ScriptScene scene in Scenes)
            {
                if (scene.Keywords == null) continue;

                scene.Keywords.Add(word);
                scene.Keywords = scene.Keywords.Select(keyword => keyword.ToLower()).ToHashSet();
            }
        }
    }

    public class ScriptScene
    {
        public string? Message { get; set; }
        public HashSet<string>? Keywords { get; set; }
    }
}
