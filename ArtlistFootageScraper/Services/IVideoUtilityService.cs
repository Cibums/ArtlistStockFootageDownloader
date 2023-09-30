using System.Drawing;

namespace ArtlistFootageScraper.Services
{
    public interface IVideoUtilityService
    {
        public OptimizedFootageData GetOptimizedFootageData(string path, float lengthSeconds);
        public string MergeVideoAndAudio(string videoPath, string audioPath);
        public List<Point> ExtractPointsBetweenIndexes(List<Point> points, int startIndex, int endIndex);
        public Point CalculateAveragePoint(List<Point> points);
        public int IndexOfMaxSumSubsequence(List<int> numbers, int length);
        public TimeSpan GetWavDuration(string filePath);
    }
}