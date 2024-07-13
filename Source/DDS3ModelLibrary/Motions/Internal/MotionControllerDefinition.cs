using DDS3ModelLibrary.IO.Common;
using System;

namespace DDS3ModelLibrary.Motions.Internal
{
    internal struct MotionControllerDefinition : IBinarySerializable, IEquatable<MotionControllerDefinition>
    {
        public ControllerType Type;
        public short Field02;
        public short NodeIndex;
        public short Field06;

        public override bool Equals(object obj)
        {
            return obj is MotionControllerDefinition definition && Equals(definition);
        }

        public bool Equals(MotionControllerDefinition other)
        {
            return Type == other.Type &&
                   Field02 == other.Field02 &&
                   NodeIndex == other.NodeIndex &&
                   Field06 == other.Field06;
        }

        public override int GetHashCode()
        {
            var hashCode = -541184802;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Field02.GetHashCode();
            hashCode = hashCode * -1521134295 + NodeIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + Field06.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(MotionControllerDefinition definition1, MotionControllerDefinition definition2)
        {
            return definition1.Equals(definition2);
        }

        public static bool operator !=(MotionControllerDefinition definition1, MotionControllerDefinition definition2)
        {
            return !(definition1 == definition2);
        }


        // -- IBinarySerializable
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Type = (ControllerType)reader.ReadUInt16();
            Field02 = reader.ReadInt16();
            NodeIndex = reader.ReadInt16();
            Field06 = reader.ReadInt16();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteInt16((short)Type);
            writer.WriteInt16(Field02);
            writer.WriteInt16(NodeIndex);
            writer.WriteInt16(Field06);
        }
    }
}