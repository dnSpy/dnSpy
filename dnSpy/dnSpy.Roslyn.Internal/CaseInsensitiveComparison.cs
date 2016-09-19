// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal
{
    /// <summary>
    /// Case-insensitive operations (mostly comparison) on unicode strings.
    /// </summary>
    public static class CaseInsensitiveComparison
    {
        // PERF: Cache a TextInfo for Unicode ToLower since this will be accessed very frequently
        private static readonly TextInfo s_unicodeCultureTextInfo = GetUnicodeCulture().TextInfo;

        private static CultureInfo GetUnicodeCulture()
        {
            try
            {
                // We use the "en" culture to get the Unicode ToLower mapping, as it implements
                // a much more recent Unicode version (6.0+) than the invariant culture (1.0),
                // and it matches the Unicode version used for character categorization.
                return new CultureInfo("en");
            }
            catch (ArgumentException) // System.Globalization.CultureNotFoundException not on all platforms
            {
                // If "en" is not available, fall back to the invariant culture. Although it has bugs
                // specific to the invariant culture (e.g. being version-locked to Unicode 1.0), at least
                // we can rely on it being present on all platforms.
                return CultureInfo.InvariantCulture;
            }
        }

        /// <summary>
        /// ToLower implements the Unicode lowercase mapping
        /// as described in ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt.
        /// VB uses these mappings for case-insensitive comparison.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>If <paramref name="c"/> is upper case, then this returns its Unicode lower case equivalent. Otherwise, <paramref name="c"/> is returned unmodified.</returns>
        public static char ToLower(char c)
        {
            // PERF: This is a very hot code path in VB, optimize for ASCII

            // Perform a range check with a single compare by using unsigned arithmetic
            if (unchecked((uint)(c - 'A')) <= ('Z' - 'A'))
            {
                return (char)(c | 0x20);
            }

            if (c < 0xC0) // Covers ASCII (U+0000 - U+007F) and up to the next upper-case codepoint (Latin Capital Letter A with Grave)
            {
                return c;
            }

            return ToLowerNonAscii(c);
        }

        private static char ToLowerNonAscii(char c)
        {
            if (c == '\u0130')
            {
                // Special case Turkish I (LATIN CAPITAL LETTER I WITH DOT ABOVE)
                // This corrects for the fact that the invariant culture only supports Unicode 1.0
                // and therefore does not "know about" this character.
                return 'i';
            }

            return s_unicodeCultureTextInfo.ToLower(c);
        }

        /// <summary>
        /// This class seeks to perform the lowercase Unicode case mapping.
        /// </summary>
        private sealed class OneToOneUnicodeComparer : StringComparer
        {
            private static int CompareLowerUnicode(char c1, char c2)
            {
                return (c1 == c2) ? 0 : ToLower(c1) - ToLower(c2);
            }

            public override int Compare(string str1, string str2)
            {
                if ((object)str1 == str2)
                {
                    return 0;
                }

                if ((object)str1 == null)
                {
                    return -1;
                }

                if ((object)str2 == null)
                {
                    return 1;
                }

                int len = Math.Min(str1.Length, str2.Length);
                for (int i = 0; i < len; i++)
                {
                    int ordDiff = CompareLowerUnicode(str1[i], str2[i]);
                    if (ordDiff != 0)
                    {
                        return ordDiff;
                    }
                }

                // return the smaller string, or 0 if they are equal in length
                return str1.Length - str2.Length;
            }

            private static bool AreEqualLowerUnicode(char c1, char c2)
            {
                return c1 == c2 || ToLower(c1) == ToLower(c2);
            }

            public override bool Equals(string str1, string str2)
            {
                if ((object)str1 == str2)
                {
                    return true;
                }

                if ((object)str1 == null || (object)str2 == null)
                {
                    return false;
                }

                if (str1.Length != str2.Length)
                {
                    return false;
                }

                for (int i = 0; i < str1.Length; i++)
                {
                    if (!AreEqualLowerUnicode(str1[i], str2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool EndsWith(string value, string possibleEnd)
            {
                if ((object)value == possibleEnd)
                {
                    return true;
                }

                if ((object)value == null || (object)possibleEnd == null)
                {
                    return false;
                }

                int i = value.Length - 1;
                int j = possibleEnd.Length - 1;

                if (i < j)
                {
                    return false;
                }

                while (j >= 0)
                {
                    if (!AreEqualLowerUnicode(value[i], possibleEnd[j]))
                    {
                        return false;
                    }

                    i--;
                    j--;
                }

                return true;
            }

            public static bool StartsWith(string value, string possibleStart)
            {
                if ((object)value == possibleStart)
                {
                    return true;
                }

                if ((object)value == null || (object)possibleStart == null)
                {
                    return false;
                }

                if (value.Length < possibleStart.Length)
                {
                    return false;
                }

                for(int i = 0; i < possibleStart.Length; i++)
                {
                    if (!AreEqualLowerUnicode(value[i], possibleStart[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode(string str)
            {
                int hashCode = Hash.FnvOffsetBias;

                for (int i = 0; i < str.Length; i++)
                {
                    hashCode = Hash.CombineFNVHash(hashCode, ToLower(str[i]));
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        private static readonly OneToOneUnicodeComparer s_comparer = new OneToOneUnicodeComparer();

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        public static StringComparer Comparer => s_comparer;

        /// <summary>
        /// Determines if two VB identifiers are equal according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>true if the identifiers should be considered the same.</returns>
        public static bool Equals(string left, string right) => s_comparer.Equals(left, right);

        /// <summary>
        /// Determines if the string 'value' end with string 'possibleEnd'.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="possibleEnd"></param>
        /// <returns></returns>
        public static bool EndsWith(string value, string possibleEnd) => OneToOneUnicodeComparer.EndsWith(value, possibleEnd);

        /// <summary>
        /// Determines if the string 'value' starts with string 'possibleStart'.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="possibleStart"></param>
        /// <returns></returns>
        public static bool StartsWith(string value, string possibleStart) => OneToOneUnicodeComparer.StartsWith(value, possibleStart);

        /// <summary>
        /// Compares two VB identifiers according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>-1 if <paramref name="left"/> &lt; <paramref name="right"/>, 1 if <paramref name="left"/> &gt; <paramref name="right"/>, 0 if they are equal.</returns>
        public static int Compare(string left, string right) => s_comparer.Compare(left, right);

        /// <summary>
        /// Gets a case-insensitive hash code for VB identifiers.
        /// </summary>
        /// <param name="value">identifier to get the hash code for</param>
        /// <returns>The hash code for the given identifier</returns>
        public static int GetHashCode(string value)
        {
            Debug.Assert(value != null);

            return s_comparer.GetHashCode(value);
        }

        /// <summary>
        /// Convert a string to lower case per Unicode
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToLower(string value)
        {
            if ((object)value == null)
                return null;

            if (value.Length == 0)
                return value;

            var pooledStrbuilder = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooledStrbuilder.Builder;

            builder.Append(value);
            ToLower(builder);

            return pooledStrbuilder.ToStringAndFree();
        }

        /// <summary>
        /// In-place convert string in StringBuilder to lower case per Unicode rules
        /// </summary>
        /// <param name="builder"></param>
        public static void ToLower(StringBuilder builder)
        {
            if (builder == null)
                return;

            for (int i = 0; i < builder.Length; i++)
            {
                builder[i] = ToLower(builder[i]);
            }
        }
    }
}
