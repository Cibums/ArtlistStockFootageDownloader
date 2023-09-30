namespace ArtlistFootageScraper.Services
{
    public class GPTResponse
    {
        public List<Choice> Choices { get; set; }
        public long Created { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public string Object { get; set; }
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        public string FinishReason { get; set; }
        public int Index { get; set; }
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Content { get; set; }
        public string Role { get; set; }
    }

    public class Usage
    {
        public int CompletionTokens { get; set; }
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}