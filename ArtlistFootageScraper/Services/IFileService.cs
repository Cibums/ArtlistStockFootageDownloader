namespace ArtlistFootageScraper.Services
{
    public interface IFileService
    {
        void AppendAllText(string file, string text);
        public string RenameFileToSnakeCase(string filePath);
        public string ConvertToSnakeCase(string input);
        void DeleteIfExists(string filePath);
        void DeleteVideoTempFiles(string outputDir);
        string? GetLatestChangedFile(string downloadDirectory);
        public void WaitForDownloadStart(string directory);
        public void WaitForDownloadCompletion(string directory);
    }
}