using System.Collections.Generic;

namespace Phar.Package
{
    /// <summary>
    /// Common helper methods.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Checks whether given <paramref name="data"/> are starts with <paramref name="prefix"/>.
        /// </summary>
        public static bool IsPrefixed(this byte[] data, byte[] prefix)
        {
            if (data == null || prefix == null)
                return false;

            if (data.Length < prefix.Length)
                return false;

            for (int i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Checks whether given <paramref name="data"/> are ends with <paramref name="suffix"/>.
        /// </summary>
        public static bool IsSuffixed(this List<byte> data, byte[] suffix)
        {
            if (data.Count < suffix.Length)
                return false;

            int datapos = data.Count - 1;
            int suffixpos = suffix.Length - 1;
            for (; suffixpos >= 0; suffixpos--, datapos--)
            {
                if (data[datapos] != suffix[suffixpos])
                    return false;
            }

            return true;
        }
    }
}
