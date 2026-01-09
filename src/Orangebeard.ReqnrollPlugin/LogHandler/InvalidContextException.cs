using System;
using System.Runtime.Serialization;

namespace Orangebeard.ReqnrollPlugin.LogHandler
{
    public class InvalidContextException : Exception
    {
        public InvalidContextException()
        {
        }
        public InvalidContextException(string message) : base(message)
        {
        }
        public InvalidContextException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}