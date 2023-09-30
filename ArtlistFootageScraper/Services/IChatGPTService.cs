namespace ArtlistFootageScraper.Services
{
    public interface IChatGPTService
    {
        public Task<GPTResponse?> CallChatGPT(string prompt);
    }
}