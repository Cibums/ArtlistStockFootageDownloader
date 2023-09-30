using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Services
{
    public interface IVideoAnalysisService
    {
        public VideoAnalysisResult AnalyzeVideo(string path, VideoCapture capture);
    }
}
