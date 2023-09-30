using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ArtlistFootageScraper.Services
{
    public class VideoAnalyzerService
    {
        private static readonly string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "stock-footage");

        public string? RenderFootage(string footagePath, string speechPath)
        {
            float length = speechPath != null ? (float)GetWavDuration(speechPath).TotalSeconds + 0.5f : 2;
            OptimizedFootageData data = GetOptimizedFootageData(footagePath, length);
            string? processedFootagePath = ProcessVideo(footagePath, data);
            return processedFootagePath != null ? MergeVideoAndAudio(processedFootagePath, speechPath) : null;

        }
        private OptimizedFootageData GetOptimizedFootageData(string path, float lengthSeconds)
        {
            VideoCapture capture = new VideoCapture(path);
            var framerate = capture.Get(CapProp.Fps);
            VideoAnalysisResult videoAnalysisResult = AnalyzeVideo(path, capture);
            int startFrame = IndexOfMaxSumSubsequence(videoAnalysisResult.ActionPerFrame, (int)Math.Round((float)framerate * lengthSeconds));
            int endFrame = startFrame + (int)Math.Round((float)framerate * lengthSeconds);
            List<Point> points = ExtractPointsBetweenIndexes(videoAnalysisResult.MaxActionCoordinatesPerFrame, startFrame, endFrame);
            Point focusPoint = points != null ? CalculateAveragePoint(points) : new Point(80, 60);
            return new OptimizedFootageData(framerate, lengthSeconds, startFrame, focusPoint);
        }

        private string? ProcessVideo(string inputPath, OptimizedFootageData data)
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

        private static string MergeVideoAndAudio(string videoPath, string audioPath)
        {
            int rndNumber = new Random().Next(0, 10000000);
            string outputP = outputPath + $"\\{rndNumber}merged_output.mp4";
            string ffmpegExecutablePath = AppConfiguration.FFMPEG_EXE_PATH + @"\ffmpeg.exe";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = ffmpegExecutablePath;
                psi.Arguments = $"-i \"{videoPath}\" -itsoffset 0.5 -i \"{audioPath}\" -c:v copy -c:a aac \"{outputP}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                Process process = new Process { StartInfo = psi };

                List<string> outputLines = new List<string>();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                        outputLines.Add(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                        Console.WriteLine("Error: " + e.Data);  // or log it, or store it for later analysis
                };

                process.Start();

                process.BeginOutputReadLine();  // Start reading output asynchronously
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                throw;
            }

            Console.WriteLine("FFmpeg process completed.");

            return outputP;
        }

        private VideoAnalysisResult AnalyzeVideo(string path, VideoCapture capture)
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

        private List<Point> ExtractPointsBetweenIndexes(List<Point> points, int startIndex, int endIndex)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }


            if (startIndex < 0 || startIndex >= points.Count || endIndex < 0 || startIndex > endIndex)
            {
                throw new ArgumentOutOfRangeException("Invalid start or end index.");
            }

            //int endFrame = Math.Min(endIndex, points.Count);
            try
            {
                List<Point> list = points.GetRange(startIndex, endIndex - startIndex);
                return list;
            }
            catch
            {
                return null;
            }
        }

        private Point CalculateAveragePoint(List<Point> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("The list of points cannot be null or empty.");

            int totalX = 0;
            int totalY = 0;

            foreach (var point in points)
            {
                totalX += point.X;
                totalY += point.Y;
            }

            return new Point(totalX / points.Count, totalY / points.Count);
        }

        private static int IndexOfMaxSumSubsequence(List<int> numbers, int length)
        {
            if (numbers == null || numbers.Count < length)
            {
                return 0;
            }

            int currentSum = numbers.Take(length).Sum();
            int maxSum = currentSum;
            int maxIndex = 0;

            for (int i = 1; i <= numbers.Count - length; i++)
            {
                currentSum = currentSum - numbers[i - 1] + numbers[i + length - 1];

                if (currentSum > maxSum)
                {
                    maxSum = currentSum;
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        public static TimeSpan GetWavDuration(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Read RIFF header
                string chunkID = new string(reader.ReadChars(4));
                if (chunkID != "RIFF")
                    throw new Exception("Invalid file format");

                reader.ReadUInt32(); // Chunk size
                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new Exception("Invalid file format");

                // Read fmt subchunk
                string subchunk1ID = new string(reader.ReadChars(4));
                if (subchunk1ID != "fmt ")
                    throw new Exception("Invalid file format");

                int subchunk1Size = reader.ReadInt32();
                short audioFormat = reader.ReadInt16();
                short numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                // Skip to data subchunk
                reader.BaseStream.Seek(subchunk1Size - 16, SeekOrigin.Current);
                string subchunk2ID = new string(reader.ReadChars(4));
                if (subchunk2ID != "data")
                    throw new Exception("Invalid file format");

                int subchunk2Size = reader.ReadInt32();

                // Calculate duration
                double totalSamples = subchunk2Size / (bitsPerSample / 8.0);
                double duration = totalSamples / sampleRate;

                return TimeSpan.FromSeconds(duration);
            }
        }
    }
}
