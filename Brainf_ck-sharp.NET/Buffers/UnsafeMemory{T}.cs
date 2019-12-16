﻿using System.Runtime.CompilerServices;
using Brainf_ck_sharp.NET.Helpers;

namespace Brainf_ck_sharp.NET.Buffers
{
    /// <summary>
    /// A <see langword="struct"/> that maps a range of <typeparamref name="T"/> values on an existing buffer
    /// </summary>
    /// <typeparam name="T">The type of items stored in the underlying buffer</typeparam>
    internal readonly unsafe struct UnsafeMemory<T> where T : unmanaged
    {
        /// <summary>
        /// The size of the usable buffer for the current instance
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// A pointer to the first element in of the underlying buffer for the current instance
        /// </summary>
        private readonly T* Ptr;

        /// <summary>
        /// Creates a new <see cref="UnsafeMemory{T}"/> instance with the specified parameters
        /// </summary>
        /// <param name="size">The size of the new memory buffer to use</param>
        /// <param name="ptr"></param>
        public UnsafeMemory(int size, T* ptr)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(size, 0, nameof(size));
            DebugGuard.MustBeFalse(ptr == null && size > 0, nameof(ptr));

            Size = size;
            Ptr = ptr;
        }

        /// <summary>
        /// Gets an empty <see cref="UnsafeMemory{T}"/> instance
        /// </summary>
        public static UnsafeMemory<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new UnsafeMemory<T>(0, null);
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> value at the specified index in the current buffer
        /// </summary>
        /// <param name="index">The target index to read the value from</param>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                DebugGuard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));
                DebugGuard.MustBeLessThan(index, Size, nameof(index));

                return Ptr[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                DebugGuard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));
                DebugGuard.MustBeLessThan(index, Size, nameof(index));

                Ptr[index] = value;
            }
        }

        /// <summary>
        /// Gets a new <see cref="UnsafeMemory{T}"/> instance representing a view over the current buffer
        /// </summary>
        /// <param name="start">The inclusive starting index for the new <see cref="UnsafeMemory{T}"/> instance</param>
        /// <param name="end">The exclusive ending index for the new <see cref="UnsafeMemory{T}"/> instance</param>
        /// <returns>A new <see cref="UnsafeMemory{T}"/> instance mapping values in the [start, end) range on the current buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeMemory<T> Slice(int start, int end)
        {
            DebugGuard.MustBeGreaterThanOrEqualTo(start, 0, nameof(start));
            DebugGuard.MustBeGreaterThanOrEqualTo(end, 0, nameof(end));
            DebugGuard.MustBeLessThan(start, Size, nameof(start));
            DebugGuard.MustBeLessThan(end, Size, nameof(end));
            DebugGuard.MustBeLessThanOrEqualTo(start, end, nameof(start));

            return new UnsafeMemory<T>(end - start, Ptr + start);
        }
    }
}