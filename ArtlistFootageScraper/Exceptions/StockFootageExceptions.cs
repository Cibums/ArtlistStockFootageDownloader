using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtlistFootageScraper.Exceptions
{
    public class StockFootageException : Exception
    {
        public StockFootageException() { }

        public StockFootageException(string message) : base(message) { }

        public StockFootageException(string message, Exception inner) : base(message, inner) { }
    }
}
