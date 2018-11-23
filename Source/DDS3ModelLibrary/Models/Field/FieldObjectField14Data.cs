using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models.Field
{
    public class FieldObjectField14Data : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Field00 { get; set; }
        public Field04Data Field04 { get; set; }
        public int Field08 { get; set; }
        public int Field0C { get; set; }

        public FieldObjectField14Data()
        {
            Field04 = new Field04Data();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Field00 = reader.ReadInt32();
            Field04 = reader.ReadObjectOffset<Field04Data>();
            Field08 = reader.ReadInt32();
            Field0C = reader.ReadInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Field00 );
            writer.ScheduleWriteObjectOffsetAligned( Field04, 16 );
            writer.Write( Field08 );
            writer.Write( Field0C );
        }

        public class Field04Data : IBinarySerializable
        {
            BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

            public int Field00 { get; set; }
            public int Field04 { get; set; }
            public int Field08 { get; set; }
            public int Field0C { get; set; }

            public Field04Data()
            {
                Field00 = 0x3130;
            }

            void IBinarySerializable.Read( EndianBinaryReader reader, object context )
            {
                Field00 = reader.ReadInt32();
                Field04 = reader.ReadInt32();
                Field08 = reader.ReadInt32();
                Field0C = reader.ReadInt32();
            }

            void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
            {
                writer.Write( Field00 );
                writer.Write( Field04 );
                writer.Write( Field08 );
                writer.Write( Field0C );
            }
        }
    }
}