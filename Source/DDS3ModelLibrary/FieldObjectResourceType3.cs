using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class FieldObjectResourceType3 : IFieldObjectResource
    {
        FieldObjectResourceType IFieldObjectResource.FieldObjectResourceType => FieldObjectResourceType.Type3;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Field00 { get; set; }
        public int Field04 { get; set; }
        public Field08Data Field08 { get; set; }
        public int Field0C { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadInt32();
            Field08 = reader.ReadObjectOffset<Field08Data>();
            Field0C = reader.ReadInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Field00 );
            writer.Write( Field04 );
            writer.ScheduleWriteObjectOffset( Field08, 16 );
            writer.Write( Field0C );
        }

        public class Field08Data : IBinarySerializable
        {
            BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

            public int Field08 { get; set; }
            public List<Vector4> Field0C { get; set; }
            public List<Field10Data> Field10 { get; set; }
            public Field14Data Field14 { get; set; }
            public int Field18 { get; set; }
            public int Field1C { get; set; }

            public Field08Data()
            {
                Field08 = 1;
            }

            void IBinarySerializable.Read( EndianBinaryReader reader, object context )
            {
                var field0CDataCount = reader.ReadInt32();
                var field10DataCount = reader.ReadInt32();
                Field08 = reader.ReadInt32();
                reader.ReadOffset( () => Field0C = reader.ReadVector4List( field0CDataCount ) );
                Field10 = reader.ReadObjectListOffset<Field10Data>( field10DataCount );
                Field14 = reader.ReadObjectOffset<Field14Data>();
                Field18 = reader.ReadInt32();
                Field1C = reader.ReadInt32();
            }

            void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
            {
                writer.Write( Field0C.Count );
                writer.Write( Field10.Count );
                writer.Write( Field08 );
                writer.ScheduleWriteOffsetAligned( 16, () => writer.Write( Field0C ) );
                writer.ScheduleWriteObjectListOffset( Field10, 16 );
                writer.ScheduleWriteObjectOffset( Field14, 16 );
                writer.Write( Field18 );
                writer.Write( Field1C );
            }

            public class Field10Data : IBinarySerializable
            {
                BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

                public int   Field00 { get; set; }
                public int   Field04 { get; set; }
                public int   Field08 { get; set; }
                public int   Field0C { get; set; }
                public int   Field10 { get; set; }
                public int   Field14 { get; set; }
                public int   Field18 { get; set; }
                public int   Field1C { get; set; }
                public short Field20 { get; set; }
                public short Field22 { get; set; }

                public Field10Data()
                {
                    Field00 = 0x8000;
                    Field20 = 1;
                    Field22 = 4;
                }

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
                    Field20 = reader.ReadInt16();
                    Field22 = reader.ReadInt16();
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
                    writer.Write( Field22 );
                }
            }
            public class Field14Data : IBinarySerializable
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
    }
}