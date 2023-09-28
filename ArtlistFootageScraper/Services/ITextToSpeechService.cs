namespace ArtlistFootageScraper.Services
{
    public interface ITextToSpeechService
    {
        Task<string> DownloadAudioFileAsync(string downloadUrl, string targetFolderPath);
        Task<string?> GetRawTTSFileAsync(string message);
        Task<Guid?> CreateSpeechFileAsync(string message);
        Task<string?> GetDownloadPathForTTSFileAsync(Guid guid);
    }
}