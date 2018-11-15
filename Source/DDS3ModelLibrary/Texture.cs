using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using DDS3ModelLibrary;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.GS;
using DDS3ModelLibrary.Texturing;
using Color = DDS3ModelLibrary.Primitives.Color;

namespace DDS3ModelLibrary
{
    public class Texture : Resource
    {
        private const int COMMENT_MAX_LENGTH = 28;

        private static BitField sWrapModeXField = new BitField( 0, 3 );
        private static BitField sWrapModeYField = new BitField( 4, 7 );

        private byte mWrapModes;
        private Bitmap mBitmap;

        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor( ResourceFileType.Texture, ResourceIdentifier.Texture );

        public byte PaletteCount => ( byte ) ( Palettes?.Count ?? 0 );

        public GSPixelFormat PaletteFormat { get; private set; }

        public ushort Width { get; private set; }

        public ushort Height { get; private set; }

        public GSPixelFormat PixelFormat { get; private set; }

        public byte MipMapCount => ( byte ) ( Math.Max( 0, ( PixelIndices?.Count ?? Pixels.Count ) - 1 ) );

        public ushort MipKL { get; set; }

        public TextureWrapMode WrapModeX
        {
            get => ( TextureWrapMode )sWrapModeXField.Unpack( mWrapModes );
            set => sWrapModeXField.Pack( ref mWrapModes, ( byte )value );
        }

        public TextureWrapMode WrapModeY
        {
            get => ( TextureWrapMode )sWrapModeYField.Unpack( mWrapModes );
            set => sWrapModeYField.Pack( ref mWrapModes, ( byte )value );
        }

        public int UserTextureId { get; set; }

        public int UserClutId { get; set; }

        public string UserComment { get; set; }

        public bool IsIndexed => GSPixelFormatHelper.IsIndexedPixelFormat( PixelFormat );

        public int PaletteColorCount =>
            ( PixelFormat == GSPixelFormat.PSMT4 || PixelFormat == GSPixelFormat.PSMT4HL || PixelFormat == GSPixelFormat.PSMT4HH ) ? 16 : 256;

        public bool HasTiledPalette => PaletteColorCount == 256;

        public List<Color[]> Palettes { get; private set; }

        public List<byte[]> PixelIndices { get; private set; }

        public List<Color[]> Pixels { get; private set; }

        public Texture()
        {
        }

        public Texture( string path )
        {
            using ( var reader = new EndianBinaryReader( path, Endianness.Little ) )
                Read( reader );
        }

        public Texture( Bitmap bitmap, GSPixelFormat pixelFormat = GSPixelFormat.PSMT8, string comment = "" )
        {
            Width       = ( ushort )bitmap.Width;
            Height      = ( ushort )bitmap.Height;
            PixelFormat = pixelFormat;
            mWrapModes  = byte.MaxValue;
            UserComment = comment;

            switch ( pixelFormat )
            {
                case GSPixelFormat.PSMTC32:
                case GSPixelFormat.PSMTC24:
                case GSPixelFormat.PSMTC16:
                case GSPixelFormat.PSMTC16S: // Non-indexed
                    PaletteFormat = 0;
                    Pixels = new List<Color[]> { ScaleAlpha( BitmapHelper.GetColors( bitmap ), GSHelper.AlphaToGSAlpha ) };
                    mBitmap       = bitmap;
                    break;
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    SetupIndexedBitmap( bitmap, 256 );
                    break;
                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    SetupIndexedBitmap( bitmap, 16 );
                    break;
                default:
                    throw new ArgumentException( "This pixel format is not supported for encoding." );
            }
        }

        public Color[] GetPixels()
        {
            if ( IsIndexed && Pixels == null )
            {
                Pixels = new List<Color[]> { new Color[Width * Height] };
                for ( int y = 0; y < Height; y++ )
                    for ( int x = 0; x < Width; x++ )
                        Pixels[0][x + y * Width] = Palettes[0][PixelIndices[0][x + y * Width]];
            }

            return Pixels[0];
        }

        public Bitmap GetBitmap( int paletteIndex = 0, int mipLevel = 0 )
        {
            if ( mBitmap == null || ( mBitmap.Width != Width && mBitmap.Height != Height ) )
            {
                CreateBitmap( paletteIndex, mipLevel );
            }

            return mBitmap;
        }

        private void SetupIndexedBitmap( Bitmap bitmap, int paletteColorCount )
        {
            BitmapHelper.QuantizeBitmap( bitmap, paletteColorCount, out var indices, out var palette );
            Palettes = new List<Color[]>() { ScaleAlpha( palette, GSHelper.AlphaToGSAlpha ) };
            PixelIndices = new List<byte[]>() { indices };
            PaletteFormat = GSPixelFormat.PSMTC32;
        }

        private static int GetMipDimension( int dim, int mipIdx )
        {
            if ( mipIdx == 0 )
                return dim;

            int div = 2 * ( 2 * mipIdx );
            return dim / div;
        }

        private static Color[] ScaleAlpha( Color[] palette, Func<byte, byte> scaler )
        {
            var newPalette = new Color[palette.Length];
            for ( int i = 0; i < palette.Length; i++ )
            {
                var c = palette[ i ];
                newPalette[ i ] = new Color( c.R, c.G, c.B, scaler( c.A ) );
            }

            return newPalette;
        }

        private void CreateBitmap( int palIdx, int mipIdx )
        {
            if ( IsIndexed )
            {
                mBitmap = BitmapHelper.Create( ScaleAlpha( Palettes[ palIdx ], GSHelper.AlphaFromGSAlpha ), PixelIndices[ mipIdx ],
                                               GetMipDimension( Width, mipIdx ), GetMipDimension( Height, mipIdx ) );
            }
            else
            {
                mBitmap = BitmapHelper.Create( ScaleAlpha( Pixels[ mipIdx ], GSHelper.AlphaFromGSAlpha ),
                                               GetMipDimension( Width, mipIdx ), GetMipDimension( Height, mipIdx ) );
            }
        }

        internal override void ReadContent( EndianBinaryReader reader, ResourceHeader header )
        {
            var paletteCount = reader.ReadByte();
            PaletteFormat = ( GSPixelFormat )reader.ReadByte();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PixelFormat = ( GSPixelFormat )reader.ReadByte();
            var mipMapCount = reader.ReadByte();
            MipKL = reader.ReadUInt16();
            var reserved = reader.ReadByteExpects( 0, "TMX reserved field isnt 0" );
            mWrapModes = reader.ReadByte();
            UserTextureId = reader.ReadInt32();
            UserClutId = reader.ReadInt32();
            UserComment = reader.ReadString( StringBinaryFormat.FixedLength, COMMENT_MAX_LENGTH );

            if ( paletteCount > 0 )
            {
                Palettes = new List<Color[]>( paletteCount );

                int paletteDimension = PaletteColorCount == 16 ? 4 : 16;

                for ( int i = 0; i < paletteCount; i++ )
                {
                    var palette = GSPixelFormatHelper.ReadPixelData<Color>( PaletteFormat, reader, paletteDimension, paletteDimension );
                    if ( HasTiledPalette )
                        palette = GSPixelFormatHelper.TilePalette( palette );

                    Palettes.Add( palette );
                }

                PixelIndices = new List<byte[]>
                {
                    GSPixelFormatHelper.ReadPixelData<byte>( PixelFormat, reader, Width, Height )
                };

                if ( mipMapCount > 0 )
                {
                    for ( int i = 0; i < mipMapCount; i++ )
                    {
                        int div = 2 * ( 2 * ( i + 1 ) );
                        PixelIndices.Add( GSPixelFormatHelper.ReadPixelData<byte>( PixelFormat, reader, Width / div, Height / div ) );
                    }
                }
            }
            else
            {
                Pixels = new List<Color[]> { GSPixelFormatHelper.ReadPixelData<Color>( PixelFormat, reader, Width, Height ) };

                if ( mipMapCount > 0 )
                {
                    for ( int i = 0; i < mipMapCount; i++ )
                    {
                        int div = 2 * ( 2 * ( i + 1 ) );
                        Pixels.Add( GSPixelFormatHelper.ReadPixelData<Color>( PixelFormat, reader, Width / div, Height / div ) );
                    }
                }
            }
        }

        internal override void WriteContent( EndianBinaryWriter writer, object context )
        {
            writer.Write( PaletteCount );
            writer.Write( ( byte )PaletteFormat );
            writer.Write( Width );
            writer.Write( Height );
            writer.Write( ( byte )PixelFormat );
            writer.Write( MipMapCount );
            writer.Write( MipKL );
            writer.Write( ( byte )0 );
            writer.Write( mWrapModes );
            writer.Write( UserTextureId );
            writer.Write( UserClutId );
            writer.Write( UserComment, StringBinaryFormat.FixedLength, COMMENT_MAX_LENGTH );

            if ( Palettes != null )
            {
                var paletteDimension = GSPixelFormatHelper.GetPaletteDimension( PaletteFormat );

                for ( int i = 0; i < Palettes.Count; i++ )
                {
                    var palette = Palettes[ i ];
                    if ( HasTiledPalette )
                        palette = GSPixelFormatHelper.TilePalette( palette );

                    GSPixelFormatHelper.WritePixelData( PaletteFormat, writer, paletteDimension, paletteDimension, palette );
                }

                for ( int i = 0; i < PixelIndices.Count; i++ )
                {
                    int div = i == 0 ? 1 : 2 * ( 2 * i );
                    GSPixelFormatHelper.WritePixelData( PixelFormat, writer, Width / div, Height / div, PixelIndices[i] );
                }

            }
            else
            {
                for ( int i = 0; i < Pixels.Count; i++ )
                {
                    int div = i == 0 ? 1 : 2 * ( 2 * i );
                    GSPixelFormatHelper.WritePixelData( PixelFormat, writer, Width / div, Height / div, Pixels[i] );
                }
            }
        }
    }
}
