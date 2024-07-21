using System;
using System.Security.Cryptography;

namespace DDS3ModelLibrary.Utilities
{
    public static class ByteArrayExtensions
    {
        public static string GetSHA256(this byte[] @this)
        {
            var hash = SHA256.HashData(@this);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
