using System;
using System.Drawing;
using System.IO;
using Color = DDS3ModelLibrary.Primitives.Color;

namespace DDS3ModelLibrary.PS2.GS
{
    internal static class GSPixelFormatHelper
    {
        private const string EXCEPTION_INVALID_PXFORMAT = "Invalid pixel format specified.";

        // read/write delegates

        public delegate Color[] ReadPixelColorDelegate( BinaryReader reader, int width, int height );

        public delegate byte[] ReadPixelIndicesDelegate( BinaryReader reader, int width, int height );

        public delegate void WritePixelColorDelegate( BinaryWriter writer, int width, int height, Color[] colorArray );

        public delegate void WritePixelIndicesDelegate( BinaryWriter writer, int width, int height, byte[] colorArray );

        // alpha scalers

        public static byte ConvertFromPS2Alpha( byte original )
        {
            return ( byte )Math.Min( ( original / 128.0f ) * 255, 255 );
        }

        public static byte ConvertToPS2Alpha( byte original )
        {
            return ( byte )( ( original / 255.0f ) * 128 );
        }

        // helper methods to get info about the pixel format

        public static int GetPaletteDimension( GSPixelFormat imageFmt )
        {
            int paletteWH = 16;

            if ( imageFmt == GSPixelFormat.PSMT4 ||
                 imageFmt == GSPixelFormat.PSMT4HH ||
                 imageFmt == GSPixelFormat.PSMT4HL )
                paletteWH = 4;

            return paletteWH;
        }

        public static int GetPixelFormatDepth( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMTC32:
                case GSPixelFormat.PSMZ32:
                case GSPixelFormat.PSMZ24:
                    return 32;
                case GSPixelFormat.PSMTC24:
                    return 24;
                case GSPixelFormat.PSMTC16:
                case GSPixelFormat.PSMTC16S:
                case GSPixelFormat.PSMZ16:
                case GSPixelFormat.PSMZ16S:
                    return 16;
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    return 8;
                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return 4;

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static int GetIndexedColorCount( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    return 256;
                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return 16;
                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static int GetTexelDataSize( GSPixelFormat fmt, int width, int height )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMTC32:
                case GSPixelFormat.PSMTC24:
                case GSPixelFormat.PSMZ32:
                case GSPixelFormat.PSMZ24:
                    return ( width * height ) * 4;

                case GSPixelFormat.PSMTC16:
                case GSPixelFormat.PSMTC16S:
                case GSPixelFormat.PSMZ16:
                case GSPixelFormat.PSMZ16S:
                    return ( width * height ) * 2;

                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    return ( width * height ) * 1;

                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return ( width * height ) / 2; // 4 bit index only takes up half a texel

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static bool IsIndexedPixelFormat( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT8H:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return true;
            }
            return false;
        }

        public static GSPixelFormat GetBestPixelFormat( Bitmap bitmap )
        {
            /*
            int similarColorCount = BitmapHelper.GetSimilarColorCount(bitmap);
            if ( similarColorCount > 50 )
                return GSPixelFormat.PSMT8;
            else
                return GSPixelFormat.PSMT4;
            */

            return GSPixelFormat.PSMT8;
        }

        // post/pre processing methods

        public static Color[] TilePalette( Color[] palette )
        {
            var newPalette = new Color[palette.Length];
            int newIndex = 0;
            int oldIndex = 0;
            for ( int i = 0; i < 8; i++ )
            {
                for ( int x = 0; x < 8; x++ )
                {
                    newPalette[newIndex++] = palette[oldIndex++];
                }
                oldIndex += 8;
                for ( int x = 0; x < 8; x++ )
                {
                    newPalette[newIndex++] = palette[oldIndex++];
                }
                oldIndex -= 16;
                for ( int x = 0; x < 8; x++ )
                {
                    newPalette[newIndex++] = palette[oldIndex++];
                }
                oldIndex += 8;
                for ( int x = 0; x < 8; x++ )
                {
                    newPalette[newIndex++] = palette[oldIndex++];
                }
            }

            return newPalette;
        }

        public static byte[] UnSwizzle8( int width, int height, byte[] paletteIndices )
        {
            var newPaletteIndices = new byte[paletteIndices.Length];
            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x++ )
                {
                    int blockLocation = ( y & ( ~0xF ) ) * width + ( x & ( ~0xF ) ) * 2;
                    int swapSelector = ( ( ( y + 2 ) >> 2 ) & 0x1 ) * 4;
                    int positionY = ( ( ( y & ( ~3 ) ) >> 1 ) + ( y & 1 ) ) & 0x7;
                    int columnLocation = positionY * width * 2 + ( ( x + swapSelector ) & 0x7 ) * 4;
                    int byteNumber = ( ( y >> 1 ) & 1 ) + ( ( x >> 2 ) & 2 ); // 0,1,2,3
                    newPaletteIndices[y * width + x] = paletteIndices[blockLocation + columnLocation + byteNumber];
                }
            }

            return newPaletteIndices;
        }

        public static byte[] Swizzle8( int width, int height, byte[] paletteIndices )
        {
            var newPaletteIndices = new byte[paletteIndices.Length];
            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x++ )
                {
                    byte uPen = paletteIndices[( y * width + x )];

                    int blockLocation = ( y & ( ~0xF ) ) * width + ( x & ( ~0xF ) ) * 2;
                    int swapSelector = ( ( ( y + 2 ) >> 2 ) & 0x1 ) * 4;
                    int positionY = ( ( ( y & ( ~3 ) ) >> 1 ) + ( y & 1 ) ) & 0x7;
                    int columnLocation = positionY * width * 2 + ( ( x + swapSelector ) & 0x7 ) * 4;
                    int byteNumber = ( ( y >> 1 ) & 1 ) + ( ( x >> 2 ) & 2 ); // 0,1,2,3

                    newPaletteIndices[blockLocation + columnLocation + byteNumber] = uPen;
                }
            }

            return newPaletteIndices;
        }

        // read/write delegate factory methods

        public static ReadPixelColorDelegate GetReadPixelColorDelegate( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMTC32:
                case GSPixelFormat.PSMZ32:
                    return ReadPSMCT32;

                case GSPixelFormat.PSMZ24:
                case GSPixelFormat.PSMTC24:
                    return ReadPSMCT24;

                case GSPixelFormat.PSMTC16:
                case GSPixelFormat.PSMZ16:
                    return ReadPSMCT16;

                case GSPixelFormat.PSMZ16S:
                case GSPixelFormat.PSMTC16S:
                    return ReadPSMCT16S;

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static ReadPixelIndicesDelegate GetReadPixelIndicesDelegate( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    return ReadPSMT8;

                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return ReadPSMT4;

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static WritePixelColorDelegate GetWritePixelColorDelegate( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMTC32:
                case GSPixelFormat.PSMZ32:
                    return WritePSMCT32;

                case GSPixelFormat.PSMZ24:
                case GSPixelFormat.PSMTC24:
                    return WritePSMCT24;

                case GSPixelFormat.PSMTC16:
                case GSPixelFormat.PSMZ16:
                    return WritePSMCT16;

                case GSPixelFormat.PSMZ16S:
                case GSPixelFormat.PSMTC16S:
                    return WritePSMCT16S;

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        public static WritePixelIndicesDelegate GetWritePixelIndicesDelegate( GSPixelFormat fmt )
        {
            switch ( fmt )
            {
                case GSPixelFormat.PSMT8:
                case GSPixelFormat.PSMT8H:
                    return WritePSMT8;

                case GSPixelFormat.PSMT4:
                case GSPixelFormat.PSMT4HL:
                case GSPixelFormat.PSMT4HH:
                    return WritePSMT4;

                default:
                    throw new ArgumentException( EXCEPTION_INVALID_PXFORMAT, nameof( fmt ) );
            }
        }

        // read methods

        public static Color[] ReadPSMCT32( BinaryReader reader, int width, int height )
        {
            var colorArray = new Color[height * width];

            for ( int i = 0; i < colorArray.Length; i++ )
            {
                uint color = reader.ReadUInt32();
                colorArray[i] = new Color( ( byte )( color & byte.MaxValue ),
                                               ( byte )( ( color >> 8 ) & byte.MaxValue ),
                                               ( byte )( ( color >> 16 ) & byte.MaxValue ),
                                                ( byte )( ( color >> 24 ) & byte.MaxValue ) );
            }

            return colorArray;
        }

        public static Color[] ReadPSMCT24( BinaryReader reader, int width, int height )
        {
            var colorArray = new Color[height * width];
            for ( int i = 0; i < colorArray.Length; i++ )
            {
                colorArray[ i ] = new Color( reader.ReadByte(),
                                             reader.ReadByte(),
                                             reader.ReadByte() );
            }

            return colorArray;
        }

        public static Color[] ReadPSMCT16( BinaryReader reader, int width, int height )
        {
            var colorArray = new Color[width * height];
            for ( int i = 0; i < colorArray.Length; i++ )
            {
                ushort color = reader.ReadUInt16();
                colorArray[ i ] = new Color( ( byte ) ( ( color & 0x001F ) << 3 ),
                                             ( byte ) ( ( ( color & 0x03E0 ) >> 5 ) << 3 ),
                                             ( byte ) ( ( ( color & 0x7C00 ) >> 10 ) << 3 ) );
            }

            return colorArray;
        }

        public static Color[] ReadPSMCT16S( BinaryReader reader, int width, int height )
        {
            var colorArray = new Color[width * height];
            for ( int i = 0; i < colorArray.Length; i++ )
            {
                short color = reader.ReadInt16();
                colorArray[ i ] = new Color( ( byte ) ( ( color & 0x001F ) << 3 ),
                                             ( byte ) ( ( ( color & 0x03E0 ) >> 5 ) << 3 ),
                                             ( byte ) ( ( ( color & 0x7C00 ) >> 10 ) << 3 ) );
            }

            return colorArray;
        }

        public static byte[] ReadPSMT8( BinaryReader reader, int width, int height )
        {
            var indicesArray = new byte[width * height];
            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x++ )
                {
                    indicesArray[x + y * width] = reader.ReadByte();
                }
            }

            return indicesArray;
        }

        public static byte[] ReadPSMT4( BinaryReader reader, int width, int height )
        {
            var indicesArray = new byte[width * height];
            for ( int y = 0; y < height; y++ )
            {
                for ( int x = 0; x < width; x += 2 )
                {
                    byte indices = reader.ReadByte();
                    indicesArray[x + y * width] = ( byte )( indices & 0x0F );
                    indicesArray[( x + 1 ) + y * width] = ( byte )( ( indices & 0xF0 ) >> 4 );
                }
            }

            return indicesArray;
        }

        // write methods

        public static void WritePSMCT32( BinaryWriter writer, int width, int height, Color[] colorArray )
        {
            foreach ( var color in colorArray )
            {
                uint colorData = ( uint )( color.R | ( color.G << 8 ) | ( color.B << 16 ) | ( color.A << 24 ) );
                writer.Write( colorData );
            }
        }

        public static void WritePSMCT24( BinaryWriter writer, int width, int height, Color[] colorArray )
        {
            foreach ( var color in colorArray )
            {
                writer.Write( color.R );
                writer.Write( color.G );
                writer.Write( color.B );
            }
        }

        public static void WritePSMCT16( BinaryWriter writer, int width, int height, Color[] colorArray )
        {
            foreach ( var color in colorArray )
            {
                int r = color.R >> 3;
                int g = color.G >> 3;
                int b = color.B >> 3;
                int a = color.A >> 7;
                ushort colorData = ( ushort )( ( a << 15 ) | ( b << 10 ) | ( g << 5 ) | ( r ) );
                writer.Write( colorData );
            }
        }

        public static void WritePSMCT16S( BinaryWriter writer, int width, int height, Color[] colorArray )
        {
            foreach ( var color in colorArray )
            {
                short colorData = ( short )( ( color.R & 0x1F ) | ( ( color.G & 0x1F ) << 8 ) | ( ( color.B & 0x1F ) << 16 ) );
                writer.Write( colorData );
            }
        }

        public static void WritePSMT8( BinaryWriter writer, int width, int height, byte[] indicesArray )
        {
            for ( int i = 0; i < indicesArray.Length; i++ )
            {
                writer.Write( indicesArray[i] );
            }
        }

        public static void WritePSMT4( BinaryWriter writer, int width, int height, byte[] indicesArray )
        {
            for ( int i = 0; i < indicesArray.Length; i += 2 )
            {
                writer.Write( ( byte )( ( indicesArray[i] & 0x0F ) | ( ( indicesArray[i + 1] & 0x0F ) << 4 ) ) );
            }
        }

        // generic read/write methods

        public static T[] ReadPixelData<T>( GSPixelFormat fmt, BinaryReader reader, int width, int height )
        {
            if ( IsIndexedPixelFormat( fmt ) )
            {
                var readPixelIndices = GetReadPixelIndicesDelegate( fmt );
                return readPixelIndices( reader, width, height ) as T[];
            }
            else
            {
                var readPixels = GetReadPixelColorDelegate( fmt );
                return readPixels( reader, width, height ) as T[];
            }
        }

        public static void WritePixelData<T>( GSPixelFormat fmt, BinaryWriter writer, int width, int height, T[] array )
        {
            if ( IsIndexedPixelFormat( fmt ) )
            {
                var writePixelIndices = GetWritePixelIndicesDelegate( fmt );
                writePixelIndices( writer, width, height, array as byte[] );
            }
            else
            {
                var writePixels = GetWritePixelColorDelegate( fmt );
                writePixels( writer, width, height, array as Color[] );
            }
        }
    }
}