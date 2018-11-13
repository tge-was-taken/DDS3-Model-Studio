using System;
using System.Runtime.Serialization;

namespace DDS3ModelLibrary
{
    [Serializable]
    internal class UnexpectedDataException : Exception
    {
        public UnexpectedDataException()
        {
        }

        public UnexpectedDataException( string message ) : base( message )
        {
        }

        public UnexpectedDataException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected UnexpectedDataException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}