using System;

namespace DDS3ModelLibrary.Materials
{
    /// <summary>
    /// Describes the different flags a material can have.
    /// </summary>
    [Flags]
    public enum MaterialFlags
    {
        Bit0 = 1 << 0,
        Bit1 = 1 << 1,
        Bit2 = 1 << 2,
        Bit3 = 1 << 3,
        Bit4 = 1 << 4,
        Bit5 = 1 << 5,
        Bit6 = 1 << 6,
        Bit7 = 1 << 7,
        Bit8 = 1 << 8,
        Bit9 = 1 << 9,
        Bit10 = 1 << 10,
        Bit11 = 1 << 11,
        Bit12 = 1 << 12,
        Bit13 = 1 << 13,
        Bit14 = 1 << 14,
        Bit15 = 1 << 15,
        Color1 = 1 << 16,
        Color2 = 1 << 17,
        TextureId = 1 << 18,
        FloatArray1 = 1 << 19,
        Color3 = 1 << 20,
        OverlayTextureIds = 1 << 21,
        FloatArray2 = 1 << 22,
        Color4 = 1 << 23,
        Color5 = 1 << 24,
        Float1 = 1 << 25,
        FloatArray3 = 1 << 26,
        Bit27 = 1 << 27,
        Bit28 = 1 << 28,
        Bit29 = 1 << 29,
        Bit30 = 1 << 30,
        Bit31 = 1 << 31,
    }
}