namespace ArtlistFootageScraper
{
    public class Storage
    {
        // The dictionary to hold href as the key and local file path as the value
        public Dictionary<string, string> Links { get; set; } = new Dictionary<string, string>();
    }
}
