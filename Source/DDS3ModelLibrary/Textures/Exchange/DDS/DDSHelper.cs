﻿using System.Drawing;
using System.IO;

namespace DDS3ModelLibrary.Textures.Exchange.DDS
{
    /// <summary>
    /// High level helper for DDS textures.
    /// </summary>
    public static class DDSHelper
    {
        /// <summary>
        /// Gets a stream containing DDS data. If the file is already in DDS format, it won't be re-encoded.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static Stream GetDDSStream(string filepath)
        {
            if (Path.GetExtension(filepath) == ".dds")
                return File.OpenRead(filepath);
            else
                return new MemoryStream(DDSCodec.CompressImage(new Bitmap(filepath)));
        }
    }
}
