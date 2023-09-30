using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace ArtlistFootageScraper.Services
{
    public class VideoAnalysisService : IVideoAnalysisService
    {
        public VideoAnalysisResult AnalyzeVideo(string path, VideoCapture capture)
        {
            VideoAnalysisResult result = new VideoAnalysisResult();
            Mat lastFrame = null;

            int totalFrames = 0;
            while (true)
            {
                Mat tempFrame = new Mat();
                if (!capture.Read(tempFrame) || tempFrame.IsEmpty)
                    break;
                totalFrames++;
            }

            capture.Dispose();
            capture = new VideoCapture(path);

            for (int i = 0; i < totalFrames; i++)
            {
                Mat frame = new Mat();
                capture.Read(frame);

                // Downscale
                CvInvoke.Resize(frame, frame, new System.Drawing.Size(160, 120));

                // Convert to grayscale
                Mat grayFrame = new Mat();
                CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

                // Compute the difference
                if (lastFrame != null)
                {
                    Mat diff = new Mat();
                    CvInvoke.AbsDiff(grayFrame, lastFrame, diff);

                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    diff.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    int difference = CvInvoke.CountNonZero(diff);
                    result.ActionPerFrame.Add(difference);
                    result.MaxActionCoordinatesPerFrame.Add(maxLocations[0]);
                }
                else
                {
                    result.ActionPerFrame.Add(0);
                    result.MaxActionCoordinatesPerFrame.Add(new Point(0, 0));
                }

                lastFrame = grayFrame;
            }

            return result;
        }
    }
}
