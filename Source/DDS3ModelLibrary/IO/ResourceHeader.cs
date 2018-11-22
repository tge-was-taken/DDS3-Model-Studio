using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public class ResourceHeader : IBinarySerializable
    {
        public const int SIZE = 16;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ResourceFileType FileType { get; set; }
        public bool IsCompressed { get; set; }
        public ushort UserId { get; set; }
        public uint FileSize { get; set; }
        public ResourceIdentifier Identifier { get; set; }
        public uint MemorySize { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            FileType = ( ResourceFileType )reader.ReadByte();
            IsCompressed = reader.ReadBoolean();
            UserId = reader.ReadUInt16();
            FileSize = reader.ReadUInt32();
            Identifier = ( ResourceIdentifier ) reader.ReadUInt32();
            MemorySize = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( ( byte ) FileType );
            writer.Write( IsCompressed );
            writer.Write( UserId );
            writer.Write( FileSize );
            writer.Write( ( uint ) Identifier );
            writer.Write( MemorySize );
        }
    }
}