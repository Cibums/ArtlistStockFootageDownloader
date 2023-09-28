using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper
{
    public class TtsJobResponseModel
    {
        public Guid Id { get; set; }
        public string? Status { get; set; }
        public int Eta { get; set; }
        public string? Text { get; set; }
    }

    public class TtsResponseModel
    {
        public Guid Id { get; set; }
        public string? Status { get; set; }
        public string? Url { get; set; }
        public int JobTime { get; set; }
    }
}
