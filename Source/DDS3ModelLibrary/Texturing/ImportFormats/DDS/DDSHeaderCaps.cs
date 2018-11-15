using System;

namespace DDS3ModelLibrary.Texturing.ImportFormats.DDS
{
    [Flags]
    public enum DDSHeaderCaps
    {
        Complex = 0x8,
        MipMap  = 0x400000,
        Texture = 0x1000,
    }
}