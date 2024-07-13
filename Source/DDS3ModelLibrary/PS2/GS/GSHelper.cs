using System;

namespace DDS3ModelLibrary.PS2.GS
{
    public static class GSHelper
    {
        public static byte AlphaFromGSAlpha(byte original)
        {
            return (byte)Math.Min((original / 128.0f) * 255, 255);
        }

        public static byte AlphaToGSAlpha(byte original)
        {
            return (byte)((original / 255.0f) * 128);
        }
    }
}
