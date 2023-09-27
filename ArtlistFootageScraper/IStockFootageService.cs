namespace ArtlistFootageScraper
{
    public interface IStockFootageService
    {
        string? GenerateStockFootageFromKeywordsSynchronously(string[] keywords, string downloadDirectory);
    }
}