using DDS3ModelLibrary.Textures.Exchange.DDS;
using System;
using System.Drawing;
using System.IO;

namespace DDS3ModelLibrary.Textures.Utilities
{
    public static class TextureImportHelper
    {
        public static Bitmap ImportBitmap(string path)
        {
            try
            {
                return new Bitmap(path);
            }
            catch (Exception)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                switch (ext)
                {
                    case ".dds":
                        return DDSCodec.DecompressImage(path);
                    default:
                        return new Bitmap(32, 32);
                }
            }
        }

        public static Texture ImportTexture(string path)
        {
            var bitmap = ImportBitmap(path);
            return new Texture(bitmap, PS2.GS.GSPixelFormat.PSMT8, Path.GetFileNameWithoutExtension(path));
        }
    }
}
