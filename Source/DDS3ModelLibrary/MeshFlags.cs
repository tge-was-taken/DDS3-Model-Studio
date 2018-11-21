using System;

namespace DDS3ModelLibrary
{
    [Flags]
    public enum MeshFlags
    {
        Bit0 = 1 << 0, // Turns model black, visual glitches
        Bit1 = 1 << 1,
        Bit2 = 1 << 2,
        SmoothShading = 1 << 3,
        TexCoord = 1 << 4,
        Bit5 = 1 << 5,
        Bit6 = 1 << 6,
        Bit7 = 1 << 7,
        Bit8 = 1 << 8,
        Bit9 = 1 << 9,
        Bit10 = 1 << 10,
        Color = 1 << 11,
        /// <summary>
        /// Used for eg. Demifiends health overlay texture.
        /// </summary>
        TexCoord2 = 1 << 12, // Verified
        Bit13 = 1 << 13,
        Bit14 = 1 << 14,
        Bit15 = 1 << 15,
        Bit16 = 1 << 16,
        Bit17 = 1 << 17,
        Bit18 = 1 << 18,
        Bit19 = 1 << 19,
        Bit20 = 1 << 20,
        RequiredForField = 1 << 21,
        Bit22 = 1 << 22,
        Normal = 1 << 23,
        FieldTexture = 1 << 24,
        Bit25 = 1 << 25,
        Bit26 = 1 << 26,
        Weights = 1 << 27,
        Bit28 = 1 << 28,
        Bit29 = 1 << 29,
        Bit30 = 1 << 30,
        Bit31 = 1 << 31,
    }
}