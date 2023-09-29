using System.Drawing;

namespace ArtlistFootageScraper.Services
{
    public class VideoAnalysisResult
    {
        public VideoAnalysisResult()
        {
            ActionPerFrame = new List<int>();
            MaxActionCoordinatesPerFrame = new List<Point>();
        }

        public VideoAnalysisResult(List<int> actionPerFrame, List<Point> maxActionCoordinatesPerFrame)
        {
            ActionPerFrame = actionPerFrame;
            MaxActionCoordinatesPerFrame = maxActionCoordinatesPerFrame;
        }

        public List<int> ActionPerFrame { get; set; }
        public List<Point> MaxActionCoordinatesPerFrame { get; set; }
    }
}