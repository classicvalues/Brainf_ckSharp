﻿using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Brainf_ckSharp.Constants;

#nullable enable

namespace System
{
    /// <summary>
    /// A <see langword="class"/> with some extension methods for the <see cref="string"/> type
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Converts a given text to the equivalent with CR line endings
        /// </summary>
        /// <param name="text">The input text to parse and convert</param>
        /// <returns>A <see cref="string"/> equivalent to <paramref name="text"/>, with CR endings</returns>
        [Pure]
        public static string WithCarriageReturnLineEndings(this string text)
        {
            if (text.Length == 0) return text;

            return text
                .Replace("\r\n", "\r")
                .Replace('\n', '\r')
                .Replace('\v', '\r');
        }

        /// <summary>
        /// Calculates the indentation depth in a given script, up to a specified index
        /// </summary>
        /// <param name="text">The input script to parse</param>
        /// <param name="end">The end index for the parsing operation</param>
        /// <returns>The indentation depth for the script at the specified index</returns>
        [Pure]
        public static int CalculateIndentationDepth(this string text, int end)
        {
            Debug.Assert(text.Length > 0);
            Debug.Assert(end >= 0);
            Debug.Assert(end < text.Length);

            ref char r0 = ref MemoryMarshal.GetReference(text.AsSpan());
            int depth = 0;

            // Only track open and closed brackets. This method assumes that
            // the input script has a valid syntax, and functions can only
            // be declared at the root level, so loops within functions
            // won't interfere with the depth counting.
            for (int i = 0; i < end; i++)
            {
                switch (Unsafe.Add(ref r0, i))
                {
                    case Characters.LoopStart: depth++; break;
                    case Characters.LoopEnd: depth--; break;
                }
            }

            return depth;
        }
    }
}
