using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper
{
    public class ScriptResponse
    {
        public string? Title { get; set; }
        public string? SoundtrackPrompt { get; set; }

        public ScriptScene[]? Scenes { get; set; }
    }

    public class ScriptScene
    {
        public string? Message { get; set; }
        public string[]? Keywords { get; set; }
    }
}
