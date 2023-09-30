namespace ArtlistFootageScraper.Services
{
    public interface IVideoProcessingService
    {
        public string? RenderFootage(string footagePath, string speechPath);
        public void ConcatenateVideos(string inputTextFilePath, string outputPath);
    }
}