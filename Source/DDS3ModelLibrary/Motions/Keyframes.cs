using DDS3ModelLibrary.IO.Common;
using System;
using System.Numerics;

namespace DDS3ModelLibrary.Motions
{
    public interface IKey : IBinarySerializable
    {
        int Size { get; }

        short Time { get; set; }
    }

    public struct UInt32Key : IKey
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteUInt32(Value);
        }
    }

    public struct PositionKeySize8 : IKey
    {
        public int Size => 8;

        public short Time { get; set; }

        public int ShapeIndex { get; set; }

        public float BlendAmount { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            ShapeIndex = reader.ReadInt32();
            BlendAmount = reader.ReadSingle();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteInt32(ShapeIndex);
            writer.WriteSingle(BlendAmount);
        }
    }

    public struct RawKey : IKey
    {
        public int Size => Data.Length;

        public short Time { get; set; }

        public byte[] Data { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            var keyFrameSize = context as int? ?? throw new ArgumentException($"Expected int as context argument");
            Data = reader.ReadBytes(keyFrameSize);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteBytes(Data);
        }
    }

    public struct Vector3Key : IKey
    {
        public int Size => 12;

        public short Time { get; set; }

        public Vector3 Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Value = reader.ReadVector3();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteVector3(Value);
        }
    }

    public struct Single2Key : IKey
    {
        public int Size => 8;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Values = reader.ReadSingleArray(2);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteSingles(Values);
        }
    }

    public struct Single5Key : IKey
    {
        public int Size => 20;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Values = reader.ReadSingleArray(5);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteSingles(Values);
        }
    }

    public struct QuaternionKey : IKey
    {
        private const float FIXED_POINT_12 = 4096f;

        private short mRotationX;
        private short mRotationY;
        private short mRotationZ;
        private short mRotationW;
        private bool mDecoded;
        private bool mChanged;
        private Quaternion mRotation;

        public int Size => 8;

        public short Time { get; set; }

        public Quaternion Value
        {
            get
            {
                if (!mChanged && !mDecoded)
                {
                    mRotation = new Quaternion(mRotationX / FIXED_POINT_12,
                                                mRotationY / FIXED_POINT_12,
                                                mRotationZ / FIXED_POINT_12,
                                                mRotationW / FIXED_POINT_12);

                    mDecoded = true;
                }

                return mRotation;
            }
            set
            {
                mRotation = value;
                mChanged = true;
            }
        }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }


        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            mRotationX = reader.ReadInt16();
            mRotationY = reader.ReadInt16();
            mRotationZ = reader.ReadInt16();
            mRotationW = reader.ReadInt16();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            if (mChanged)
            {
                mRotationX = (short)(Value.X * FIXED_POINT_12);
                mRotationY = (short)(Value.Y * FIXED_POINT_12);
                mRotationZ = (short)(Value.Z * FIXED_POINT_12);
                mRotationW = (short)(Value.W * FIXED_POINT_12);
                mChanged = false;
                mDecoded = true;
            }

            writer.Write(mRotationX);
            writer.Write(mRotationY);
            writer.Write(mRotationZ);
            writer.Write(mRotationW);
        }
    }

    public struct ByteKey : IKey
    {
        public int Size => 1;

        public short Time { get; set; }

        // maybe a bool?
        public byte Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Value = reader.ReadByte();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteByte(Value);
        }
    }

    public struct SingleKey : IKey
    {
        public int Size => 4;

        public short Time { get; set; }

        public float Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Value = reader.ReadSingle();
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteSingle(Value);
        }
    }
}
