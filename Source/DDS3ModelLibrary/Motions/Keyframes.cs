using System;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Motions
{
    public interface IKeyframe : IBinarySerializable
    {
        int Size { get; }

        short Time { get; set; }
    }

    public struct TranslationKeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public struct TranslationKeyframeSize8 : IKeyframe
    {
        public int Size => 8;

        public short Time { get; set; }

        public int ShapeIndex { get; set; }

        public float BlendAmount { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            ShapeIndex = reader.ReadInt32();
            BlendAmount = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt32( ShapeIndex );
            writer.WriteSingle( BlendAmount );
        }
    }

    public struct TranslationKeyframeSize12 : IKeyframe
    {
        public int Size => 12;

        public short Time { get; set; }

        public Vector3 Translation { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Translation = reader.ReadVector3();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteVector3( Translation );
        }
    }

    public struct Type1KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public struct ScaleKeyframeSize12 : IKeyframe
    {
        public int Size => 12;

        public short Time { get; set; }

        public Vector3 Scale { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Scale = reader.ReadVector3();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteVector3( Scale );
        }
    }

    public struct ScaleKeyframeSize20 : IKeyframe
    {
        public int Size => 20;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Values = reader.ReadSingles( 5 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingles( Values );
        }
    }

    public struct RotationKeyframeSize8 : IKeyframe
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

        public Quaternion Rotation
        {
            get
            {
                if ( !mDecoded )
                {
                    mRotation = new Quaternion( mRotationX / FIXED_POINT_12,
                                                mRotationY / FIXED_POINT_12,
                                                mRotationZ / FIXED_POINT_12,
                                                mRotationW / FIXED_POINT_12 );

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


        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            mRotationX = reader.ReadInt16();
            mRotationY = reader.ReadInt16();
            mRotationZ = reader.ReadInt16();
            mRotationW = reader.ReadInt16();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            if ( mChanged )
            {
                mRotationX = ( short )( Rotation.X * FIXED_POINT_12 );
                mRotationY = ( short ) ( Rotation.Y * FIXED_POINT_12 );
                mRotationZ = ( short ) ( Rotation.Z * FIXED_POINT_12 );
                mRotationW = ( short ) ( Rotation.W * FIXED_POINT_12 );
                mChanged = false;
            }

            writer.Write( mRotationX );
            writer.Write( mRotationY );
            writer.Write( mRotationZ );
            writer.Write( mRotationW );
        }
    }

    public struct RotationKeyframeSize20 : IKeyframe
    {
        public int Size => 20;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Values = reader.ReadSingles( 5 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingles( Values );
        }
    }

    public struct MorphKeyframeSize1 : IKeyframe
    {
        public int Size => 1;

        public short Time { get; set; }

        // maybe a bool?
        public byte Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadByte();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteByte( Value );
        }
    }

    public struct MorphKeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public struct Type5KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public float Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingle( Value );
        }
    }

    public struct Type8KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public float Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Value = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingle( Value );
        }
    }
}
