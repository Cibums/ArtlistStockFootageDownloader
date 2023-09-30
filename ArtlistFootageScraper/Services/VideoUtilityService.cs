using Emgu.CV.CvEnum;
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
    public class VideoUtilityService : IVideoUtilityService
    {
        private static readonly string outputPath = AppConfiguration.stockFootageOutputPath;
        private readonly IVideoAnalysisService _analysisService;

        public VideoUtilityService()
        {
            _analysisService = new VideoAnalysisService();
        }

        public OptimizedFootageData GetOptimizedFootageData(string path, float lengthSeconds)
        {
            VideoCapture capture = new VideoCapture(path);
            var framerate = capture.Get(CapProp.Fps);
            VideoAnalysisResult videoAnalysisResult = _analysisService.AnalyzeVideo(path, capture);
            int startFrame = IndexOfMaxSumSubsequence(videoAnalysisResult.ActionPerFrame, (int)Math.Round((float)framerate * lengthSeconds));
            int endFrame = startFrame + (int)Math.Round((float)framerate * lengthSeconds);
            List<Point> points = ExtractPointsBetweenIndexes(videoAnalysisResult.MaxActionCoordinatesPerFrame, startFrame, endFrame);
            Point focusPoint = points != null ? CalculateAveragePoint(points) : new Point(80, 60);
            return new OptimizedFootageData(framerate, lengthSeconds, startFrame, focusPoint);
        }

        public string MergeVideoAndAudio(string videoPath, string audioPath)
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

        public List<Point> ExtractPointsBetweenIndexes(List<Point> points, int startIndex, int endIndex)
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

        public Point CalculateAveragePoint(List<Point> points)
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

        public int IndexOfMaxSumSubsequence(List<int> numbers, int length)
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

        public TimeSpan GetWavDuration(string filePath)
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
