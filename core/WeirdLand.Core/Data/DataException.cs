using System;

namespace WeirdLand.Core.Data
{
    public class DataException : Exception
    {
        public DataException(string message) : base(message) { }
        public DataException(string message, Exception inner) : base(message, inner) { }
    }
}
