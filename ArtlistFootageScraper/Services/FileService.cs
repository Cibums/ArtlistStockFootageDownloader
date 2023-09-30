using System;
using System.Collections.Generic;
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

        public string? GetLatestDownloadedFile(string downloadDirectory)
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
