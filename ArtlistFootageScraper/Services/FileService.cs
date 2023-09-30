using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class FileService : IFileService
    {
        public void AppendAllText(string file, string text)
        {
            if (string.IsNullOrEmpty(file)) return;
            if (string.IsNullOrEmpty(text)) return;
            File.AppendAllText(file, text);
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
    }
}
