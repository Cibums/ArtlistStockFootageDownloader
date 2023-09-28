using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Exceptions
{
    public class MaxRetriesException : Exception
    {
        public MaxRetriesException() : base() { }
        public MaxRetriesException(string message) : base(message) { }
        public MaxRetriesException(string message, Exception innerException) : base(message, innerException) { }
    }
}
