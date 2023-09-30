namespace ArtlistFootageScraper.Services
{
    public interface IFileService
    {
        void AppendAllText(string file, string text);
        void DeleteIfExists(string filePath);
        void DeleteVideoTempFiles(string outputDir);
        string? GetLatestChangedFile(string downloadDirectory);
    }
}