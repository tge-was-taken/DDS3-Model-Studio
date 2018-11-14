using System;
using System.Diagnostics;
using System.Linq;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;

namespace DDS3ModelLibrary
{
    /// <summary>
    /// Represents a surface material to be used by meshes.
    /// </summary>
    public class Material : IBinarySerializable
    {
        // For debugging only, only valid when read from a file.
        private int mIndex;

        /* 16 */ private Color? mColor1;
        /* 17 */ private Color? mColor2;
        /* 18 */ private int? mTextureId;
        /* 19 */ private float[] mFloatArray1;
        /* 20 */ private Color? mColor3;
        /* 21 */ private short[] mShortArray;
        /* 22 */ private float[] mFloatArray2;
        /* 23 */ private Color? mColor4;
        /* 24 */ private Color? mColor5;
        /* 25 */ private float? mFloat1;
        /* 26 */ private float[] mFloatArray3;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the material flags.
        /// </summary>
        public MaterialFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets Color1.
        /// </summary>
        public Color? Color1
        {
            get => mColor1;
            set { mColor1 = value; UpdateFlags( mColor1, MaterialFlags.Color1 ); }
        }

        /// <summary>
        /// Gets or sets Color2.
        /// </summary>
        public Color? Color2
        {
            get => mColor2;
            set { mColor2 = value; UpdateFlags( mColor2, MaterialFlags.Color2 ); }
        }

        /// <summary>
        /// Gets or sets the texture id used by this material.
        /// </summary>
        public int? TextureId
        {
            get => mTextureId;
            set { mTextureId = value; UpdateFlags( mTextureId, MaterialFlags.TextureId ); }
        }

        /// <summary>
        /// Gets or sets FloatArray1.
        /// </summary>
        public float[] FloatArray1
        {
            get => mFloatArray1;
            set { mFloatArray1 = value; UpdateFlags( mFloatArray1, MaterialFlags.FloatArray1 ); }
        }

        /// <summary>
        /// Gets or sets Color3.
        /// </summary>
        public Color? Color3
        {
            get => mColor3;
            set { mColor3 = value; UpdateFlags( mColor3, MaterialFlags.Color3 ); }
        }

        /// <summary>
        /// Gets or sets ShortArray.
        /// </summary>
        public short[] ShortArray
        {
            get => mShortArray;
            set { mShortArray = value; UpdateFlags( mShortArray, MaterialFlags.ShortArray ); }
        }

        /// <summary>
        /// Gets or sets FloatArray2.
        /// </summary>
        public float[] FloatArray2
        {
            get => mFloatArray2;
            set { mFloatArray2 = value; UpdateFlags( mFloatArray2, MaterialFlags.FloatArray2 ); }
        }

        /// <summary>
        /// Gets or sets Color4.
        /// </summary>
        public Color? Color4
        {
            get => mColor4;
            set { mColor4 = value; UpdateFlags( mColor4, MaterialFlags.Color4 ); }
        }

        /// <summary>
        /// Gets or sets Color5.
        /// </summary>
        public Color? Color5
        {
            get => mColor5;
            set { mColor5 = value; UpdateFlags( mColor5, MaterialFlags.Color5 ); }
        }


        /// <summary>
        /// Gets or sets Float1.
        /// </summary>
        public float? Float1
        {
            get => mFloat1;
            set { mFloat1 = value; UpdateFlags( mFloat1, MaterialFlags.Float1 ); }
        }

        /// <summary>
        /// Gets or sets FloatArray3.
        /// </summary>
        public float[] FloatArray3
        {
            get => mFloatArray3;
            set { mFloatArray3 = value; UpdateFlags( mFloatArray3, MaterialFlags.FloatArray3 ); }
        }

        public Material()
        {
        }

        public override int GetHashCode() => GetHashCode( true );

        public int GetCacheHashCode() => GetHashCode( false );

        private int GetHashCode( bool includeIds )
        {
            var hash = 0x33333333;
            hash ^= Flags.GetHashCode();

            for ( int i = 0; i < 31; i++ )
            {
                var flag = ( 1 << i );
                if ( ( ( int )Flags & flag ) != 0 )
                {
                    switch ( ( MaterialFlags )flag )
                    {
                        case MaterialFlags.Color1:
                            hash ^= Color1.Value.GetHashCode();
                            break;
                        case MaterialFlags.Color2:
                            hash ^= Color2.Value.GetHashCode();
                            break;
                        case MaterialFlags.TextureId:
                            if ( includeIds )
                                hash ^= TextureId.Value.GetHashCode();
                            break;
                        case MaterialFlags.FloatArray1:
                            hash = FloatArray1.Aggregate( hash, ( current, f ) => current ^ f.GetHashCode() );
                            break;
                        case MaterialFlags.Color3:
                            hash ^= Color3.Value.GetHashCode();
                            break;
                        case MaterialFlags.ShortArray:
                            hash = ShortArray.Aggregate( hash, ( current, v ) => current ^ v );
                            break;
                        case MaterialFlags.FloatArray2:
                            hash = FloatArray2.Aggregate( hash, ( current, f ) => current ^ f.GetHashCode() );
                            break;
                        case MaterialFlags.Color4:
                            hash ^= Color4.Value.GetHashCode();
                            break;
                        case MaterialFlags.Color5:
                            hash ^= Color5.Value.GetHashCode();
                            break;
                        case MaterialFlags.Float1:
                            hash ^= Float1.Value.GetHashCode();
                            break;
                        case MaterialFlags.FloatArray3:
                            hash = FloatArray3.Aggregate( hash, ( current, f ) => current ^ f.GetHashCode() );
                            break;
                    }
                }
            }

            return hash;
        }

        private void UpdateFlags( object value, MaterialFlags flag )
        {
            if ( value == null )
                Flags &= ~flag;
            else
                Flags |= flag;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            mIndex = reader.ReadInt32();
            var flags = ( MaterialFlags )reader.ReadInt32();
            Flags = flags;

            for ( int i = 0; i < 31; i++ )
            {
                var flag = ( 1 << i );
                if ( ( ( int )Flags & flag ) != 0 )
                {
                    switch ( ( MaterialFlags )flag )
                    {
                        case MaterialFlags.Color1:
                            Color1 = reader.ReadColor();
                            break;
                        case MaterialFlags.Color2:
                            Color2 = reader.ReadColor();
                            break;
                        case MaterialFlags.TextureId:
                            TextureId = reader.ReadInt32();
                            break;
                        case MaterialFlags.FloatArray1:
                            FloatArray1 = reader.ReadSingles( 5 );
                            break;
                        case MaterialFlags.Color3:
                            Color3 = reader.ReadColor();
                            break;
                        case MaterialFlags.ShortArray:
                            ShortArray = reader.ReadInt16Array( 2 );
                            break;
                        case MaterialFlags.FloatArray2:
                            FloatArray2 = reader.ReadSingles( 5 );
                            break;
                        case MaterialFlags.Color4:
                            Color4 = reader.ReadColor();
                            break;
                        case MaterialFlags.Color5:
                            Color5 = reader.ReadColor();
                            break;
                        case MaterialFlags.Float1:
                            Float1 = reader.ReadSingle();
                            break;
                        case MaterialFlags.FloatArray3:
                            FloatArray3 = reader.ReadSingles( 2 );
                            break;
                        default:
                            throw new UnexpectedDataException( "Unknown material flag" );
                    }
                }
            }

            Debug.Assert( Flags == flags );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var index = ( int )context;
            writer.Write( index );
            writer.Write( ( int )Flags );

            for ( int i = 0; i < 31; i++ )
            {
                var flag = ( 1 << i );
                if ( ( ( int )Flags & flag ) != 0 )
                {
                    switch ( ( MaterialFlags )flag )
                    {
                        case MaterialFlags.Color1:
                            writer.Write( Color1.Value );
                            break;
                        case MaterialFlags.Color2:
                            writer.Write( Color2.Value );
                            break;
                        case MaterialFlags.TextureId:
                            writer.Write( TextureId.Value );
                            break;
                        case MaterialFlags.FloatArray1:
                            writer.Write( FloatArray1 );
                            break;
                        case MaterialFlags.Color3:
                            writer.Write( Color3.Value );
                            break;
                        case MaterialFlags.ShortArray:
                            writer.Write( ShortArray );
                            break;
                        case MaterialFlags.FloatArray2:
                            writer.Write( FloatArray2 );
                            break;
                        case MaterialFlags.Color4:
                            writer.Write( Color4.Value );
                            break;
                        case MaterialFlags.Color5:
                            writer.Write( Color5.Value );
                            break;
                        case MaterialFlags.Float1:
                            writer.Write( Float1.Value );
                            break;
                        case MaterialFlags.FloatArray3:
                            writer.Write( FloatArray3 );
                            break;
                        default:
                            throw new ArgumentOutOfRangeException( "Unknown material flag" );
                    }
                }
            }
        }
    }
}