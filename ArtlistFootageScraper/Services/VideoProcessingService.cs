using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;

namespace ArtlistFootageScraper.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private string FFMPEG_EXECUTABLE_PATH = AppConfiguration.FFMPEG_EXE_PATH;
        private readonly string _outputPath = AppConfiguration.stockFootageOutputPath;
        private readonly IVideoUtilityService _utilityService;
        private readonly IFileService _fileService; // Assume an IFileService interface exists
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(IVideoUtilityService utilityService, IFileService fileService, ILogger<VideoProcessingService> logger)
        {
            _utilityService = utilityService ?? throw new ArgumentNullException(nameof(utilityService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string? RenderFootage(string footagePath, string speechPath)
        {
            _logger.LogInformation($"Rendering footage from path: {footagePath} with speech from path: {speechPath}");

            float length = !string.IsNullOrEmpty(speechPath)
                ? (float)_utilityService.GetWavDuration(speechPath).TotalSeconds + 0.5f
                : 2;

            var data = _utilityService.GetOptimizedFootageData(footagePath, length);
            var processedFootagePath = ProcessVideo(footagePath, data);

            var resultPath = processedFootagePath != null
                ? _utilityService.MergeVideoAndAudio(processedFootagePath, speechPath)
                : null;

            if (resultPath == null)
            {
                _logger.LogWarning($"Failed to render footage from path: {footagePath}");
            }

            return resultPath;
        }

        private string? ProcessVideo(string inputPath, OptimizedFootageData data)
        {
            _logger.LogInformation($"Processing video from path: {inputPath}");

            if (data.Fps != 25)
            {
                string originalName = Path.GetFileName(inputPath);
                string newInputPath = ConvertTo25fps(inputPath, _outputPath + "\\convertedFps_" + originalName);
                inputPath = newInputPath;
                data.Fps = 25;
            }

            using var capture = new VideoCapture(inputPath);
            var cropRect = CalculateCropRectangle(capture, data);

            string outputFile = $"{_outputPath}/output.mp4";

            WriteCroppedVideo(capture, cropRect, data, outputFile);

            if (string.IsNullOrEmpty(outputFile))
            {
                _logger.LogWarning($"Failed to process video from path: {inputPath}");
            }

            return _fileService.GetLatestChangedFile(_outputPath);
        }

        private Rectangle CalculateCropRectangle(VideoCapture capture, OptimizedFootageData data)
        {
            _logger.LogInformation($"Calculating Focus Point");
            int originalWidth = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
            int originalHeight = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);

            int desiredWidth = (int)(originalHeight * 9.0 / 16.0);
            int startX = ((int)((double)data.FocusPoint.X / 160 * originalWidth)) - desiredWidth / 2;
            int startY = (originalHeight / 2) - originalHeight / 2;

            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            desiredWidth = (startX + desiredWidth > originalWidth) ? (originalWidth - startX) : desiredWidth;

            return new Rectangle(startX, startY, desiredWidth, originalHeight);
        }

        private void WriteCroppedVideo(VideoCapture capture, Rectangle cropRect, OptimizedFootageData data, string outputFile)
        {
            _logger.LogInformation($"Cropping Video");

            int fourCC = VideoWriter.Fourcc('H', '2', '6', '4');

            using var writer = new VideoWriter(outputFile, fourCC, data.Fps, cropRect.Size, true);
            Mat frame = new Mat();
            int frameCount = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
            capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, data.StartFrame);

            for (int i = 0; i < frameCount; i++)
            {
                if (i < data.StartFrame) continue;
                if (i > data.StartFrame + (int)Math.Round(data.LengthSecond * data.Fps)) break;

                capture.Read(frame);
                if (!frame.IsEmpty)
                {
                    using var croppedFrame = new Mat(frame, cropRect);
                    writer.Write(croppedFrame);
                }
            }
        }

        public void ConcatenateVideos(string inputTextFilePath, string outputPath)
        {
            _logger.LogInformation($"Concatenating Videos");
            ExecuteFFmpeg($"-f concat -safe 0 -i \"{inputTextFilePath}\" -c copy \"{outputPath}\"");
        }

        string ConvertTo25fps(string inputVideoPath, string outputVideoPath)
        {
            ExecuteFFmpeg($"-i \"{inputVideoPath}\" -vf \"fps=25\" -c:a copy \"{outputVideoPath}\"");
            return outputVideoPath;
        }

        private void ExecuteFFmpeg(string arguments)
        {
            _logger.LogInformation($"Executing FFmpeg with arguments: {arguments}");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $"{FFMPEG_EXECUTABLE_PATH}/ffmpeg.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.WriteLine($"Error: {e.Data}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            _logger.LogInformation("FFmpeg process completed.");
        }
    }
}