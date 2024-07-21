﻿using System;

namespace DDS3ModelLibrary.Textures.Exchange.DDS
{
    [Flags]
    public enum DDSPixelFormatFlags
    {
        AlphaPixels = 0x1,
        Alpha = 0x2,
        FourCC = 0x4,
        RGB = 0x40,
        YUV = 0x200,
        Luminance = 0x20000
    }
}