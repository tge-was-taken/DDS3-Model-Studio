using System;

namespace DDS3ModelLibrary.IO.Common
{
    public static unsafe class Unsafe
    {
        public static TDest ReinterpretCast<TSource, TDest>(TSource source)
            where TSource : unmanaged
            where TDest : unmanaged
        {
            return *(TDest*)&source;
        }

        public static void ReinterpretCast<TSource, TDest>(TSource source, out TDest destination)
            where TSource : unmanaged
            where TDest : unmanaged
        {
            destination = *(TDest*)&source;
        }
    }
}
