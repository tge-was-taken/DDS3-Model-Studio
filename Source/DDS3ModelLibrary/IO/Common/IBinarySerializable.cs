namespace DDS3ModelLibrary.IO.Common
{
    /// <summary>
    /// Common interface for all objects that can be serialized from and to a stream.
    /// </summary>
    public interface IBinarySerializable
    {
        /// <summary>
        /// Gets or sets the binary source info. Can be null.
        /// </summary>
        BinarySourceInfo SourceInfo { get; set; }

        /// <summary>
        /// Reads the object from a stream using the provided reader.
        /// </summary>
        /// <param name="reader">The reader to read with.</param>
        /// <param name="context">Custom context data to be used during reading.</param>
        void Read( EndianBinaryReader reader, object context = null );

        /// <summary>
        /// Writes the object to a stream using the provided writer.
        /// </summary>
        /// <param name="writer">The writer to writer with.</param>
        /// <param name="context">Custom context data to be used during writing.</param>
        void Write( EndianBinaryWriter writer, object context = null );
    }

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
            SourceFilePath = filePath;
            SourceOffset = offset;
            SourceEndianness = endianness;
        }
    }
}
