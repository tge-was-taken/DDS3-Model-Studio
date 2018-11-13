using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class ModelExtensionHeader : IBinarySerializable
    {
        public const int SIZE = 8;

        public ModelExtensionIdentifier Identifier { get; set; }

        public int Size { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Identifier = ( ModelExtensionIdentifier )reader.ReadInt32();
            Size       = reader.ReadInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( ( int ) Identifier );
            writer.Write( Size );
        }
    }
}