namespace DDS3ModelLibrary.IO.Common
{
    public class BinarySourceInfo
    {
        /// <summary>
        /// Path to the file from which the object was read.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Offset from which the object was read.
        /// </summary>
        public long SourceOffset { get; }

        /// <summary>
        /// Endiannes in which the object was read.
        /// </summary>
        public Endianness SourceEndianness { get; }

        internal BinarySourceInfo( string filePath, long offset, Endianness endianness )
        {
            SourceFilePath   = filePath;
            SourceOffset     = offset;
            SourceEndianness = endianness;
        }
    }
}