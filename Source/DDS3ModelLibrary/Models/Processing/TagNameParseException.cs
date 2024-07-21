using System;
using System.Runtime.Serialization;

namespace DDS3ModelLibrary.Models.Processing
{
    [Serializable]
    internal class TagNameParseException : Exception
    {
        public TagNameParseException()
        {
        }

        public TagNameParseException(string message) : base(message)
        {
        }

        public TagNameParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}