namespace ArtlistFootageScraper.Services
{
    public interface IStockFootageService
    {
        string? GenerateStockFootageFromKeywordsSynchronously(string[] keywords, string downloadDirectory);
    }
}