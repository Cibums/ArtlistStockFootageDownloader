using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace ArtlistFootageScraper.Services
{
    public class VideoAnalysisService : IVideoAnalysisService
    {
        private readonly ILogger<VideoAnalysisService> _logger;

        public VideoAnalysisService(ILogger<VideoAnalysisService> logger)
        {
            _logger = logger;
        }

        public VideoAnalysisResult AnalyzeVideo(string path, VideoCapture capture)
        {
            _logger.LogInformation($"Starting video analysis for {path}");

            var result = new VideoAnalysisResult();
            int totalFrames = GetTotalFrameCount(capture);

            using (var newCapture = new VideoCapture(path))
            {
                Mat lastFrame = null;
                for (int i = 0; i < totalFrames; i++)
                {
                    using (var frame = new Mat())
                    {
                        newCapture.Read(frame);
                        var grayFrame = ConvertToGrayScale(ResizeFrame(frame));

                        if (lastFrame != null)
                        {
                            AnalyzeFrameDifference(grayFrame, lastFrame, result);
                            lastFrame.Dispose();
                        }
                        else
                        {
                            result.ActionPerFrame.Add(0);
                            result.MaxActionCoordinatesPerFrame.Add(new Point(0, 0));
                        }

                        lastFrame = grayFrame;
                    }
                }
            }

            _logger.LogInformation($"Completed video analysis for {path}");
            return result;
        }

        private int GetTotalFrameCount(VideoCapture capture)
        {
            int frameCount = 0;
            using (var tempFrame = new Mat())
            {
                while (capture.Read(tempFrame) && !tempFrame.IsEmpty)
                {
                    frameCount++;
                }
            }
            return frameCount;
        }

        private Mat ResizeFrame(Mat frame)
        {
            var resizedFrame = new Mat();
            CvInvoke.Resize(frame, resizedFrame, new Size(160, 120));
            return resizedFrame;
        }

        private Mat ConvertToGrayScale(Mat frame)
        {
            var grayFrame = new Mat();
            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
            return grayFrame;
        }

        private void AnalyzeFrameDifference(Mat currentFrame, Mat lastFrame, VideoAnalysisResult result)
        {
            using (var diff = new Mat())
            {
                CvInvoke.AbsDiff(currentFrame, lastFrame, diff);

                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;
                diff.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                int difference = CvInvoke.CountNonZero(diff);
                result.ActionPerFrame.Add(difference);
                result.MaxActionCoordinatesPerFrame.Add(maxLocations[0]);
            }
        }
    }
}