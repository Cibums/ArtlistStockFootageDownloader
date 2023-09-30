using System.Drawing;

namespace ArtlistFootageScraper
{
    public class OptimizedFootageData
    {
        public OptimizedFootageData(double fps, float length, int startFrame, Point focusPoint)
        {
            Fps = fps;
            LengthSecond = length;
            StartFrame = startFrame;
            FocusPoint = focusPoint;
        }

        public double Fps { get; set; }
        public float LengthSecond { get; set; }
        public int StartFrame { get; set; }
        public Point FocusPoint { get; set; }
    }
}