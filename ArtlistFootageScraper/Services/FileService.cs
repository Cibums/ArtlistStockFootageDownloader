using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger _logger;

        public FileService(ILogger<IFileService> logger)
        {
            _logger = logger;
        }

        public void AppendAllText(string file, string text)
        {
            if (string.IsNullOrEmpty(file)) return;
            if (string.IsNullOrEmpty(text)) return;
            File.AppendAllText(file, text);
        }

        public string RenameFileToSnakeCase(string filePath)
        {
            // Get the directory, filename without extension, and extension
            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            // Convert the filename to snake case
            string snakeCaseFileName = ConvertToSnakeCase(fileNameWithoutExtension);

            // Construct the new file path
            string newFilePath = Path.Combine(directory, snakeCaseFileName + extension);

            // Rename the file by moving it to the new path
            if (!File.Exists(newFilePath))
            {
                File.Move(filePath, newFilePath);
            }

            return newFilePath;
        }

        public string ConvertToSnakeCase(string input)
        {
            string lowerCaseInput = input.ToLower();
            string alphanumericInput = Regex.Replace(lowerCaseInput, @"[^\w\s]", string.Empty);
            string snakeCaseOutput = alphanumericInput.Replace(' ', '_');
            return snakeCaseOutput;
        }

        public void DeleteIfExists(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("File path invalid");
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        public void DeleteVideoTempFiles(string directoryPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory {directoryPath} does not exist.");
                return;
            }

            // Get all files in the directory
            string[] files = Directory.GetFiles(directoryPath);

            foreach (string file in files)
            {
                // Check if the filename contains the specified substring
                if (Path.GetFileName(file).Contains("merged") || Path.GetFileName(file).Contains("convertedFps"))
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting {file}. Reason: {ex.Message}");
                    }
                }
            }
        }

        public string? GetLatestChangedFile(string downloadDirectory)
        {
            // Find the latest downloaded file
            var latestFile = Directory.GetFiles(downloadDirectory)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestFile != null)
            {
                return latestFile.FullName;
            }
            else
            {
                Console.WriteLine("No files found in the download directory.");
            }

            return null;
        }

        public void WaitForDownloadStart(string directory)
        {
            var initialFiles = new HashSet<string>(Directory.GetFiles(directory));
            var end = DateTime.Now.AddMinutes(1);  // 1 minute timeout
            while (DateTime.Now < end)
            {
                var currentFiles = new HashSet<string>(Directory.GetFiles(directory));
                if (currentFiles.Count > initialFiles.Count ||
                    currentFiles.Except(initialFiles).Any(f => f.EndsWith(".crdownload")))
                    return;
                Thread.Sleep(500); // Check every half-second
            }
            _logger.LogError("Download did not start within expected time.");
            throw new TimeoutException("Download did not start within expected time.");
        }

        public void WaitForDownloadCompletion(string directory)
        {
            var end = DateTime.Now.AddMinutes(5);  // 5 minutes timeout, adjust as needed
            while (DateTime.Now < end)
            {
                if (!Directory.GetFiles(directory, "*.crdownload").Any())
                    return;
                Thread.Sleep(1000); // Check every second
            }
            _logger.LogError("Download did not complete within expected time.");
            throw new TimeoutException("Download did not complete within expected time.");
        }
    }
}
