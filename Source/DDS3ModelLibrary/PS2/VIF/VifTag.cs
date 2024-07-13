using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.PS2.VIF
{
    public class VifTag : IBinarySerializable
    {
        public const int SIZE = 4;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ushort Immediate;
        public byte Count;
        public byte Command;

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Immediate = reader.ReadUInt16();
            Count = reader.ReadByte();
            Command = reader.ReadByte();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.Write(Immediate);
            writer.Write(Count);
            writer.Write((byte)Command);
        }
    }
}