using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private static readonly string outputPath = AppConfiguration.stockFootageOutputPath;
        private readonly IVideoUtilityService _utilityService;

        public VideoProcessingService()
        {
            _utilityService = new VideoUtilityService();
        }

        public string? RenderFootage(string footagePath, string speechPath)
        {
            float length = speechPath != null ? (float)_utilityService.GetWavDuration(speechPath).TotalSeconds + 0.5f : 2;
            OptimizedFootageData data = _utilityService.GetOptimizedFootageData(footagePath, length);
            string? processedFootagePath = ProcessVideo(footagePath, data);
            return processedFootagePath != null ? _utilityService.MergeVideoAndAudio(processedFootagePath, speechPath) : null;

        }

        private static string? ProcessVideo(string inputPath, OptimizedFootageData data)
        {
            using (VideoCapture capture = new VideoCapture(inputPath))
            {
                int originalWidth = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
                int originalHeight = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);

                // Calculate the width based on the 16:9 ratio
                int desiredWidth = (int)(originalHeight * 9.0 / 16.0);
                int desiredHeight = originalHeight;

                // Calculate top-left corner of the crop rectangle
                int startX = ((int)((double)data.FocusPoint.X / 160 * originalWidth)) - desiredWidth / 2;
                int startY = (originalHeight / 2) - desiredHeight / 2;

                // Ensure the rectangle is within the video frame dimensions
                startX = Math.Max(startX, 0);
                startY = Math.Max(startY, 0);
                desiredWidth = (startX + desiredWidth > originalWidth) ? (originalWidth - startX) : desiredWidth;
                desiredHeight = (startY + desiredHeight > originalHeight) ? (originalHeight - startY) : desiredHeight;

                // Create the crop rectangle
                Rectangle cropRect = new Rectangle(startX, startY, desiredWidth, desiredHeight);

                double fps = capture.Get(Emgu.CV.CvEnum.CapProp.Fps);
                int fourCC = VideoWriter.Fourcc('H', '2', '6', '4'); // Using x264 codec for MP4

                using (VideoWriter writer = new VideoWriter(outputPath + "/output.mp4", fourCC, fps, new Size(desiredWidth, desiredHeight), true))
                {
                    Mat frame = new Mat();
                    int frameCount = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, data.StartFrame);
                    for (int i = 0; i < frameCount; i++)
                    {
                        if (i < data.StartFrame) continue; // Skip frames before the start frame
                        if (i > data.StartFrame + (int)Math.Round(data.LengthSecond * data.Fps)) break;     // Exit loop after the end frame

                        capture.Read(frame);
                        if (!frame.IsEmpty)
                        {
                            Mat croppedFrame = new Mat(frame, cropRect);
                            writer.Write(croppedFrame);
                        }
                    }
                }
            }

            FileService fileService = new FileService();
            return fileService.GetLatestDownloadedFile(outputPath);
        }

        public void ConcatenateVideos(string inputTextFilePath, string outputPath)
        {
            string ffmpegExecutablePath = AppConfiguration.FFMPEG_EXE_PATH + @"\ffmpeg.exe";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = ffmpegExecutablePath;
            psi.Arguments = $"-f concat -safe 0 -i \"{inputTextFilePath}\" -c copy \"{outputPath}\"";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            Process process = new Process { StartInfo = psi };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine("Error: " + e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();  // Start reading output asynchronously
            process.BeginErrorReadLine();
            process.WaitForExit();

            Console.WriteLine("FFmpeg process completed.");
        }
    }
}
