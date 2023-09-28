namespace ArtlistFootageScraper
{
    public class Storage
    {
        // The dictionary to hold href as the key and local file path as the value
        public Dictionary<string, string> FootageLinks { get; set; } = new Dictionary<string, string>();

        // The dictionary to hold TTS input as the key and local file path as the value
        public Dictionary<string, string> SpeechLinks { get; set; } = new Dictionary<string, string>();
    }
}
