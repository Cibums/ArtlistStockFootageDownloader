using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public class FileService : IFileService
    {
        public string? GetLatestDownloadedFile(string downloadDirectory)
        {
            // Find the latest downloaded file
            var latestFile = Directory.GetFiles(downloadDirectory)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
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
