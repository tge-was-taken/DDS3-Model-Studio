using DDS3ModelLibrary.IO.Common;
using System;

namespace DDS3ModelLibrary.Motions.Internal
{
    internal struct MotionControllerDefinition : IBinarySerializable, IEquatable<MotionControllerDefinition>
    {
        public ControllerType Type;
        public int NodeIndex;

        public override bool Equals(object obj)
        {
            return obj is MotionControllerDefinition definition && Equals(definition);
        }

        public bool Equals(MotionControllerDefinition other)
        {
            return Type == other.Type &&
                   NodeIndex == other.NodeIndex;
        }

        public override int GetHashCode()
        {
            var hashCode = -541184802;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + NodeIndex.GetHashCode();
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
            Type = (ControllerType)reader.ReadInt32();
            NodeIndex = reader.ReadInt32();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteInt32((int)Type);
            writer.WriteInt32(NodeIndex);
        }
    }
}