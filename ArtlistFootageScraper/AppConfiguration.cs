using DotNetEnv;

namespace ArtlistFootageScraper
{
    public static class AppConfiguration
    {
        public static readonly string stockFootageOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "stock-footage");
        public static string FIRST_NAME { get; private set; } = "";
        public static string EMAIL { get; private set; } = "";
        public static string PASSWORD { get; private set; } = "";
        public static string RAPID_API_KEY { get; private set; } = "";
        public static string FFMPEG_EXE_PATH { get; private set; } = "";
        public static string OPENAI_API_KEY { get; private set; } = "";

        static AppConfiguration()
        {
            // Load environment variables for login details
            string? parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
            if (parentDirectory == null)
            {
                Console.WriteLine("Parent directory not found. Cannot proceed without a valid directory.");
                return;
            }

            string envFilePath = Path.Combine(parentDirectory, ".env");
            Env.Load(envFilePath);

            EMAIL = Env.GetString("EMAIL");
            PASSWORD = Env.GetString("PASSWORD");
            FIRST_NAME = Env.GetString("FIRST_NAME");
            RAPID_API_KEY = Env.GetString("RAPID_API_KEY");
            FFMPEG_EXE_PATH = Env.GetString("FFMPEG_EXE_PATH");
            OPENAI_API_KEY = Env.GetString("OPENAI_API_KEY");
        }
    }
}
