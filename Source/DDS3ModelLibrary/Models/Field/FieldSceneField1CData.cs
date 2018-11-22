using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models.Field
{
    public class FieldSceneField1CData : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Field00 { get; set; }
        public int Field04 { get; set; }
        public int Field08 { get; set; }
        public int Field0C { get; set; }
        public int Field10 { get; set; }
        public int Field14 { get; set; }
        public int Field18 { get; set; }
        public int Field1C { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadInt32();
            Field08 = reader.ReadInt32();
            Field0C = reader.ReadInt32();
            Field10 = reader.ReadInt32();
            Field14 = reader.ReadInt32();
            Field18 = reader.ReadInt32();
            Field1C = reader.ReadInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Field00 );
            writer.Write( Field04 );
            writer.Write( Field08 );
            writer.Write( Field0C );
            writer.Write( Field10 );
            writer.Write( Field14 );
            writer.Write( Field18 );
            writer.Write( Field1C );
        }
    }
}