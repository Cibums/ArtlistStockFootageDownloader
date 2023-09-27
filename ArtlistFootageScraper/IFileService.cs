namespace ArtlistFootageScraper
{
    public interface IFileService
    {
        string? GetLatestDownloadedFile(string downloadDirectory);
    }
}