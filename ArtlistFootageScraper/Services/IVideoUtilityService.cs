using System.Drawing;

namespace ArtlistFootageScraper.Services
{
    public interface IVideoUtilityService
    {
        /// <summary>
        /// Gets optimized footage data.
        /// </summary>
        OptimizedFootageData GetOptimizedFootageData(string path, float lengthSeconds);

        /// <summary>
        /// Merges video and audio.
        /// </summary>
        string MergeVideoAndAudio(string videoPath, string audioPath, string? output = null);

        /// <summary>
        /// Extracts points between given indexes.
        /// </summary>
        List<Point> ExtractPointsBetweenIndexes(List<Point> points, int startIndex, int endIndex);

        /// <summary>
        /// Calculates the average point.
        /// </summary>
        Point CalculateAveragePoint(List<Point> points);

        /// <summary>
        /// Finds the index of the maximum sum subsequence.
        /// </summary>
        int IndexOfMaxSumSubsequence(List<int> numbers, int length);

        /// <summary>
        /// Gets the duration of a WAV file.
        /// </summary>
        TimeSpan GetWavDuration(string filePath);
    }
}