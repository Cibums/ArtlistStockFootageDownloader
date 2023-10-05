namespace ArtlistFootageScraper.Services
{
    public interface IVideoProcessingService
    {
        /// <summary>
        /// Renders the video footage based on the provided paths.
        /// </summary>
        /// <param name="footagePath">The path to the video footage.</param>
        /// <param name="speechPath">The path to the speech audio.</param>
        /// <returns>The path to the rendered footage, or null if the process fails.</returns>
        string? RenderFootage(string footagePath, string speechPath);

        /// <summary>
        /// Concatenates multiple videos using a provided text file containing paths to the videos.
        /// </summary>
        /// <param name="inputTextFilePath">The path to the text file containing paths to the videos to concatenate.</param>
        /// <param name="outputPath">The path where the concatenated video should be saved.</param>
        void ConcatenateVideos(string inputTextFilePath, string outputPath);

        public string CutAudioAndAdjustVolume(string audioFileName, float seconds, float volume = 0.3f);
        string AddMusicToVideo(string inputVideo, string musicFilePath, string? output = null);
    }
}