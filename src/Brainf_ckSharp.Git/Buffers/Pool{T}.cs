﻿using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// A <see langword="class"/> that implements a pool of items of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of items to store in the pool</typeparam>
    /// <remarks>The items pool is not thread safe</remarks>
    public static class Pool<T> where T : class, new()
    {
        /// <summary>
        /// The minimum size of the items pool
        /// </summary>
        private const int MinimumPoolSize = 8;

        /// <summary>
        /// The current pool of items
        /// </summary>
        /// <remarks>Not using the array pool here since previous pools are no longer needed</remarks>
        private static T[] _Items = new T[MinimumPoolSize];

        /// <summary>
        /// The current offset into the pool of items
        /// </summary>
        private static int _Offset;

        /// <summary>
        /// Initializes the static <see cref="Pool{T}"/> type
        /// </summary>
        static Pool()
        {
            ref T r0 = ref _Items[0];
            int length = _Items.Length;

            // Initialize the items in the pool
            for (int i = 0; i < length; i++)
            {
                Unsafe.Add(ref r0, i) = new T();
            }
        }

        /// <summary>
        /// Rents a new <typeparamref name="T"/> instance
        /// </summary>
        /// <returns>A recycled <typeparamref name="T"/> instance to use</returns>
        [Pure]
        public static T Rent()
        {
            // Expand the current pool, if needed
            if (_Offset == _Items.Length)
            {
                T[] oldItems = _Items;
                T[] newItems = new T[oldItems.Length * 2];

                // Copy over the previous elements
                oldItems.AsSpan().CopyTo(newItems);

                _Items = newItems;

                ref T r0 = ref newItems[0];
                int end = newItems.Length;

                for (int i = _Offset; i < end; i++)
                {
                    Unsafe.Add(ref r0, i) = new T();
                }
            }

            return _Items[_Offset++];
        }

        /// <summary>
        /// Resets the current pool of items
        /// </summary>
        /// <remarks>This can cause previously rented objects to be reused</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset() => _Offset = 0;
    }
}