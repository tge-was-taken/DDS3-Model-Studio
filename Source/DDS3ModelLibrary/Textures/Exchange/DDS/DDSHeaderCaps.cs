using System;

namespace DDS3ModelLibrary.Textures.Exchange.DDS
{
    [Flags]
    public enum DDSHeaderCaps
    {
        Complex = 0x8,
        MipMap  = 0x400000,
        Texture = 0x1000,
    }
}