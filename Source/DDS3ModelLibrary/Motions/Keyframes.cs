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
            Time = ( short )context;
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public class TranslationKeyframeSize8 : IKeyframe
    {
        public int Size => 8;

        public short Time { get; set; }

        public int ShapeIndex { get; set; }

        public float BlendAmount { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time = ( short )context;
            ShapeIndex = reader.ReadInt32();
            BlendAmount = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt32( ShapeIndex );
            writer.WriteSingle( BlendAmount );
        }
    }

    public class TranslationKeyframeSize12 : IKeyframe
    {
        public int Size => 12;

        public short Time { get; set; }

        public Vector3 Translation { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time = ( short )context;
            Translation = reader.ReadVector3();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteVector3( Translation );
        }
    }

    public class Type1KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time  = ( short )context;
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public class ScaleKeyframeSize12 : IKeyframe
    {
        public int Size => 12;

        public short Time { get; set; }

        public Vector3 Scale { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time = ( short )context;
            Scale = reader.ReadVector3();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteVector3( Scale );
        }
    }

    public class ScaleKeyframeSize20 : IKeyframe
    {
        public int Size => 20;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time   = ( short )context;
            Values = reader.ReadSingles( 5 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingles( Values );
        }
    }

    public class RotationKeyframeSize8 : IKeyframe
    {
        private const float FIXED_POINT_12 = 4096f;

        public int Size => 8;

        public short Time { get; set; }

        public Quaternion Rotation { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time = ( short )context;
            Rotation = new Quaternion( reader.ReadInt16() / FIXED_POINT_12,
                                       reader.ReadInt16() / FIXED_POINT_12,
                                       reader.ReadInt16() / FIXED_POINT_12,
                                       reader.ReadInt16() / FIXED_POINT_12 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt16( ( short )( Rotation.X * FIXED_POINT_12 ) );
            writer.WriteInt16( ( short )( Rotation.Y * FIXED_POINT_12 ) );
            writer.WriteInt16( ( short )( Rotation.Z * FIXED_POINT_12 ) );
            writer.WriteInt16( ( short )( Rotation.W * FIXED_POINT_12 ) );
        }
    }

    public class RotationKeyframeSize20 : IKeyframe
    {
        public int Size => 20;

        public short Time { get; set; }

        public float[] Values { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time  = ( short )context;
            Values = reader.ReadSingles( 5 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingles( Values );
        }
    }

    public class MorphKeyframeSize1 : IKeyframe
    {
        public int Size => 1;

        public short Time { get; set; }

        // maybe a bool?
        public byte Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time  = ( short )context;
            Value = reader.ReadByte();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteByte( Value );
        }
    }

    public class MorphKeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public uint Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time = ( short )context;
            Value = reader.ReadUInt32();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteUInt32( Value );
        }
    }

    public class Type5KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public float Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time  = ( short )context;
            Value = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingle( Value );
        }
    }

    public class Type8KeyframeSize4 : IKeyframe
    {
        public int Size => 4;

        public short Time { get; set; }

        public float Value { get; set; }

        // -- IBinarySerializable --
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Time  = ( short )context;
            Value = reader.ReadSingle();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteSingle( Value );
        }
    }
}
