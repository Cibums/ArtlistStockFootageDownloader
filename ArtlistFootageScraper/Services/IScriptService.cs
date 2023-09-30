namespace ArtlistFootageScraper.Services
{
    public interface IScriptService
    {
        public Task<ScriptResponse?> GetScript(string keyword, int videoLength);
    }
}