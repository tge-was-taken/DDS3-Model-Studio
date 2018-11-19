using System.Diagnostics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class FieldLight : IFieldObjectResource
    {
        FieldObjectResourceType IFieldObjectResource.FieldObjectResourceType => FieldObjectResourceType.Light;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Always 0.
        /// </summary>
        public int   Field00 { get; set; }
        public int   Field04 { get; set; }
        public int   Field08 { get; set; }
        public float Field0C { get; set; }
        public float Field10 { get; set; }
        public float Field14 { get; set; }
        public int   Field18 { get; set; }
        public float Field1C { get; set; }
        public float Field20 { get; set; }
        public float Field24 { get; set; }
        public float Field28 { get; set; }
        public float Field2C { get; set; }
        public float Field30 { get; set; }

        public FieldLight()
        {
            Field0C = 200f;
            Field10 = 350f;
            Field14 = 5;
            Field1C = 0.6f;
            Field20 = Field24 = 0.65f;
            Field28 = Field2C = Field30 = 0.4f;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Field00 = reader.ReadInt32(); // always 0
            Field04 = reader.ReadInt32(); // 0, 4
            Field08 = reader.ReadInt32(); // 0, 1
            Field0C = reader.ReadSingle();
            Field10 = reader.ReadSingle();
            Field14 = reader.ReadSingle();
            Field18 = reader.ReadInt32();
            Field1C = reader.ReadSingle();
            Field20 = reader.ReadSingle();
            Field24 = reader.ReadSingle();
            Field28 = reader.ReadSingle();
            Field2C = reader.ReadSingle();
            Field30 = reader.ReadSingle();
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
            writer.Write( Field30 );
        }
    }
}