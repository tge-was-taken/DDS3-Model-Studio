using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models.Field
{
    public class FieldEffect : IFieldObjectResource
    {
        FieldObjectResourceType IFieldObjectResource.FieldObjectResourceType => FieldObjectResourceType.Effect;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int   Field00 { get; set; }
        public int   Field04 { get; set; }
        public int   Field08 { get; set; }
        public float Field0C { get; set; }
        public float Field10 { get; set; }
        public float Field14 { get; set; }
        public int   Field18 { get; set; }
        public int   Field1C { get; set; }
        public int   Field20 { get; set; }
        public int   Field24 { get; set; }
        public int   Field28 { get; set; }
        public int   Field2C { get; set; }

        public FieldEffect()
        {
            Field04 = 2;
            Field0C = Field10 = 39.37f;
            Field10 = 1;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadInt32();
            Field08 = reader.ReadInt32();
            Field0C = reader.ReadSingle();
            Field10 = reader.ReadSingle();
            Field14 = reader.ReadSingle();
            Field18 = reader.ReadInt32();
            Field1C = reader.ReadInt32();
            Field20 = reader.ReadInt32();
            Field24 = reader.ReadInt32();
            Field28 = reader.ReadInt32();
            Field2C = reader.ReadInt32();
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
            writer.Write( Field20 );
            writer.Write( Field24 );
            writer.Write( Field28 );
            writer.Write( Field2C );
        }
    }
}