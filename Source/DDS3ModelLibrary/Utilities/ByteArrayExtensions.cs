using System;
using System.Security.Cryptography;

namespace DDS3ModelLibrary.Utilities
{
    public static class ByteArrayExtensions
    {
        public static string GetSHA256(this byte[] @this)
        {
            var sha = new SHA256Managed();
            var hash = sha.ComputeHash(@this);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
