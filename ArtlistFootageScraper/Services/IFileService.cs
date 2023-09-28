namespace ArtlistFootageScraper.Services
{
    public interface IFileService
    {
        string? GetLatestDownloadedFile(string downloadDirectory);
    }
}