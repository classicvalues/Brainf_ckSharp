﻿using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Brainf_ck_sharp.NET
{
    /// <summary>
    /// A <see langword="class"/> responsible for parsing and validating Brainf*ck/PBrain scripts
    /// </summary>
    public static class Brainf_ckParser
    {
        /// <summary>
        /// The maximum valid index in <see cref="OperatorsLookupTable"/>
        /// </summary>
        public static readonly int OperatorsLookupTableMaxIndex = 94;

        /// <summary>
        /// A lookup table to quickly check characters
        /// </summary>
        public static ReadOnlySpan<byte> OperatorsLookupTable => new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // ()+,-.
            0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // :<>
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0  // []
        };

        /// <summary>
        /// Checks whether or not an input character is a Brainf*ck/PBrain operator
        /// </summary>
        /// <param name="c">The input character to check</param>
        /// <returns><see langword="true"/> if the input character is a Brainf*ck/PBrain operator, <see langword="false"/> otherwise</returns>
        [Pure]
        public static bool IsOperator(char c)
        {
            int
                sign = ((OperatorsLookupTableMaxIndex - c) >> 31) & 1,
                offset = OperatorsLookupTableMaxIndex * sign + c * (sign ^ 1);
            ref byte r0 = ref MemoryMarshal.GetReference(OperatorsLookupTable);
            byte r1 = Unsafe.Add(ref r0, offset);

            return r1 != 0;
        }

        /// <summary>
        /// Checks whether or not the syntax of the input script is valid
        /// </summary>
        /// <param name="code">The input script to validate</param>
        /// <returns><see langword="true"/> if the input script has a valid syntax, <see langword="false"/> otherwise</returns>
        [Pure]
        public static bool IsSyntaxValid(string code)
        {
            // Local variables to track the depth and the function definitions
            int
                rootDepth = 0,
                functionStart = -1,
                functionDepth = 0,
                functionOps = 0,
                opsCount = 0;

            for (int i = 0; i < code.Length; i++)
            {
                switch (code[i])
                {
                    case '+':
                    case '-':
                    case '>':
                    case '<':
                    case '.':
                    case ',':
                    case ':':

                        /* For action operators, simply increase the counter if the current
                         * parser is inside a function definition. The counter is used to
                         * validate function definition without having to iterate again
                         * over the span of characters contained in the definition */
                        opsCount++;
                        if (functionStart != -1) functionOps++;
                        break;
                    case '[':

                        // Increase the appropriate depth level
                        opsCount++;
                        if (functionStart == -1) rootDepth++;
                        else
                        {
                            functionDepth++;
                            functionOps++;
                        }
                        break;
                    case ']':

                        /* Decrease the current depth level, either in the standard
                         * code flow or inside a function definition. If the current
                         * depth level is already 0, the source code is invalid */
                        opsCount++;
                        if (functionStart == -1)
                        {
                            if (rootDepth == 0) return false;
                            rootDepth--;
                        }
                        else
                        {
                            if (functionDepth == 0) return false;
                            functionDepth--;
                            functionOps++;
                        }
                        break;
                    case '(':

                        // Start a function definition, track the index and reset the counter
                        opsCount++;
                        if (functionStart != -1) return false;
                        functionStart = i;
                        functionOps = 0;
                        break;
                    case ')':

                        // Validate the function definition and reset the index
                        opsCount++;
                        if (functionStart == -1) return false;
                        if (functionOps == 0) return false;
                        functionStart = -1;
                        break;
                }
            }

            return true;
        }
    }
}