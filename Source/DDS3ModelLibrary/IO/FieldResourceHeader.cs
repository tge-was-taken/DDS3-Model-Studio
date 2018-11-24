using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public class FieldResourceHeader : IBinarySerializable
    {
        public const int SIZE = 20;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ResourceFileType FileType { get; set; }
        public ResourceIdentifier Identifier { get; set; }
        public uint DataSize { get; set; }
        public uint RelocationTableOffset { get; set; }
        public uint RelocationTableSize { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            FileType = ( ResourceFileType )reader.ReadInt32();
            Identifier = ( ResourceIdentifier )reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            RelocationTableOffset = reader.ReadUInt32();
            RelocationTableSize = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( ( int )FileType );
            writer.Write( ( uint )Identifier );
            writer.Write( DataSize );
            writer.Write( RelocationTableOffset );
            writer.Write( RelocationTableSize );
        }
    }
}