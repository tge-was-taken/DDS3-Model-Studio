using System;
using System.Drawing;
using System.IO;
using DDS3ModelLibrary.Texturing.ImportFormats.DDS;

namespace DDS3ModelLibrary.Texturing
{
    public static class TextureImportHelper
    {
        public static Bitmap ImportAsBitmap( string path )
        {
            try
            {
                return new Bitmap( path );
            }
            catch ( Exception )
            {
                var ext = Path.GetExtension( path ).ToLowerInvariant();
                switch ( ext )
                {
                    case ".dds":
                        return DDSCodec.DecompressImage( path );
                    default:
                        return new Bitmap( 32, 32 );
                }
            }
        }

        public static Texture ImportAsTexture( string path )
        {
            var bitmap = ImportAsBitmap( path );
            return new Texture( bitmap, PS2.GS.GSPixelFormat.PSMT8, Path.GetFileNameWithoutExtension( path ) );
        }
    }
}
