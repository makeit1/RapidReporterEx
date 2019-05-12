using System;

namespace Rapid_Reporter
{
    public class InvalidDirecotoryException : Exception
    {
        public InvalidDirecotoryException() : base() { }
        public InvalidDirecotoryException(string message) : base(message) { }
        public InvalidDirecotoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}