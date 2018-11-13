using System;

namespace DDS3ModelLibrary
{
    [Flags]
    public enum MeshFlags
    {
        Bit0      = 1 << 0,
        Bit1      = 1 << 1,
        Bit2      = 1 << 2,
        Bit3      = 1 << 3,
        TexCoords = 1 << 4, // Verified
        Normals   = 1 << 5, // May be wrong
        Bit6      = 1 << 6,
        Bit7      = 1 << 7,
        Bit8      = 1 << 8,
        Bit9      = 1 << 9,
        Bit10     = 1 << 10,
        Colors    = 1 << 11,
        TexCoord2     = 1 << 12,
        Bit13     = 1 << 13,
        NoNormals = 1 << 14, // May be wrong
        Bit15     = 1 << 15,
        Bit16     = 1 << 16,
        Bit17     = 1 << 17,
        Bit18     = 1 << 18,
        Bit19     = 1 << 19,
        Bit20     = 1 << 20,
        Bit21     = 1 << 21,
        Bit22     = 1 << 22,
        Bit23     = 1 << 23,
        Bit24     = 1 << 24,
        Bit25     = 1 << 25,
        Bit26     = 1 << 26,

        /// <summary>
        /// Corrupts player_b.PB shoes if not used.
        /// </summary>
        FixShoes     = 1 << 27,
        Bit28     = 1 << 28,
        Bit29     = 1 << 29,
        Bit30     = 1 << 30,
        Bit31     = 1 << 31,
    }
}