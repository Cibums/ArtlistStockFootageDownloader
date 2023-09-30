using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArtlistFootageScraper.Services
{
    public class VideoUtilityService : IVideoUtilityService
    {
        private static readonly string outputPath = AppConfiguration.stockFootageOutputPath;
        private readonly IVideoAnalysisService _analysisService;
        private readonly ILogger<VideoUtilityService> _logger;

        public VideoUtilityService(IVideoAnalysisService analysisService, ILogger<VideoUtilityService> logger)
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets optimized footage data.
        /// </summary>
        public OptimizedFootageData GetOptimizedFootageData(string path, float lengthSeconds)
        {
            var capture = new VideoCapture(path);
            var framerate = capture.Get(CapProp.Fps);
            var videoAnalysisResult = _analysisService.AnalyzeVideo(path, capture);
            var startFrame = IndexOfMaxSumSubsequence(videoAnalysisResult.ActionPerFrame, (int)Math.Round(framerate * lengthSeconds));
            var endFrame = startFrame + (int)Math.Round(framerate * lengthSeconds);
            var points = ExtractPointsBetweenIndexes(videoAnalysisResult.MaxActionCoordinatesPerFrame, startFrame, endFrame);
            var focusPoint = points != null && points.Any() ? CalculateAveragePoint(points) : new Point(80, 60);

            return new OptimizedFootageData(framerate, lengthSeconds, startFrame, focusPoint);
        }

        /// <summary>
        /// Merges video and audio.
        /// </summary>
        public string MergeVideoAndAudio(string videoPath, string audioPath)
        {
            var rndNumber = new Random().Next(0, 10000000);
            var outputFilePath = Path.Combine(outputPath, $"{rndNumber}merged_output.mp4");
            var ffmpegExecutablePath = Path.Combine(AppConfiguration.FFMPEG_EXE_PATH, "ffmpeg.exe");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegExecutablePath,
                    Arguments = $"-i \"{videoPath}\" -itsoffset 0.5 -i \"{audioPath}\" -c:v copy -c:a aac \"{outputFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogInformation(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogError($"Error: {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during merging video and audio: {ex.Message}");
                throw;
            }

            _logger.LogInformation("FFmpeg process completed.");
            return outputFilePath;
        }

        /// <summary>
        /// Extracts points between given indexes.
        /// </summary>
        public List<Point> ExtractPointsBetweenIndexes(List<Point> points, int startIndex, int endIndex)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (startIndex < 0 || startIndex >= points.Count || endIndex < 0 || startIndex > endIndex)
                throw new ArgumentOutOfRangeException("Invalid start or end index.");

            try
            {
                return points.GetRange(startIndex, endIndex - startIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting points: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculates the average point.
        /// </summary>
        public Point CalculateAveragePoint(List<Point> points)
        {
            if (points == null || !points.Any())
                throw new ArgumentException("The list of points cannot be null or empty.");

            var totalX = points.Sum(point => point.X);
            var totalY = points.Sum(point => point.Y);

            return new Point(totalX / points.Count, totalY / points.Count);
        }

        /// <summary>
        /// Finds the index of the maximum sum subsequence.
        /// </summary>
        public int IndexOfMaxSumSubsequence(List<int> numbers, int length)
        {
            if (numbers == null || numbers.Count < length)
                return 0;

            var currentSum = numbers.Take(length).Sum();
            var maxSum = currentSum;
            var maxIndex = 0;

            for (var i = 1; i <= numbers.Count - length; i++)
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
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

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
            int sampleRate = reader.ReadInt32();
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
