﻿using System.Runtime.CompilerServices;
using Brainf_ckSharp.Helpers;

namespace Brainf_ckSharp.Extensions.Types
{
    /// <summary>
    /// A <see langword="struct"/> that represents an interval of indices in a given sequence
    /// </summary>
    internal readonly struct Range
    {
        /// <summary>
        /// The starting index for the current instance
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// The ending index for the current instance
        /// </summary>
        public readonly int End;

        /// <summary>
        /// Creates a new <see cref="Range"/> instance with the specified parameters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public Range(int start, int end)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(start, 0, nameof(start));
            DebugGuard.MustBeGreaterThanOrEqualTo(end, 0, nameof(end));
            DebugGuard.MustBeLessThanOrEqualTo(start, end, nameof(start));

            Start = start;
            End = end;
        }

        /// <summary>
        /// Gets the length of the current instance
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => End - Start;
        }
    }
}