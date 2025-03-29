using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Viewer.IContract
{
    public readonly unsafe struct UnSafeArray<T>(T* ptr, int length) where T : unmanaged
    {

        public readonly unsafe T* Ptr => ptr;

        public readonly int Length => length;

        public Span<T> Span => new(Ptr, Length);

        public ReadOnlySpan<T> ReadOnlySpan => new(Ptr, Length);

        public Span<T> Slice(int start, int length)
        {
            return new Span<T>(Ptr + start, length);
        }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < Length, $"Index out of range: {index}");
                return ref Ptr[index];
            }
        }


    }

}
