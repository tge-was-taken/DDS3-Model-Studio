using System.Numerics;

namespace DDS3ModelLibrary.Models.Utilities
{
    // ReSharper disable once InconsistentNaming
    public static class Matrix4x4Extensions
    {
        public static Matrix4x4 Inverted(this Matrix4x4 value)
        {
            Matrix4x4.Invert(value, out var inverted);
            return inverted;
        }
    }
}
