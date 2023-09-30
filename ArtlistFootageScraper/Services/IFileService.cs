﻿namespace ArtlistFootageScraper.Services
{
    public interface IFileService
    {
        void AppendAllText(string file, string text);
        void DeleteIfExists(string filePath);
        string? GetLatestChangedFile(string downloadDirectory);
    }
}