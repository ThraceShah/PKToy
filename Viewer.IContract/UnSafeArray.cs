using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Viewer.IContract
{
    public readonly unsafe struct UnSafeArray<T> : IDisposable where T:unmanaged
    {

        public readonly unsafe T* Ptr;

        public readonly long Length;

        public Span<T> Span => new(Ptr, (int)Length);

        public Span<T> Span2
        {
            get
            {
                if(Length>int.MaxValue)
                {
                    return new Span<T>(Ptr+int.MaxValue, (int)(Length-int.MaxValue));
                }
                else
                {
                    return new Span<T>(Ptr, (int)Length);
                }
            }
        }

        public Span<T> Slice(int start, int length)
        {
            return new Span<T>(Ptr + start, length);
        }

        internal UnSafeArray(T* pointer, uint length)
        {
            Ptr = pointer;
            Length = length;
        }

        public T this[uint index]
        {
            get
            {
                return Ptr[index];
            }
            set
            {
                Ptr[index] = value;
            }
        }

        public static implicit operator UnSafeArray<T>(T[] array)
        {
            return UnSafeArrayEx.CreateUnSafeArray(array);
        }

        public static implicit operator UnSafeArray<T>(in ReadOnlySpan<T> array)
        {
            return UnSafeArrayEx.CreateUnSafeArray(array);
        }

        public static implicit operator UnSafeArray<T>(in Span<T> array)
        {
            return UnSafeArrayEx.CreateUnSafeArray(array);
        }


        public static implicit operator UnSafeArray<T>(in ArraySegment<T> array)
        {
            return UnSafeArrayEx.CreateUnSafeArray(array);
        }



        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)Ptr);
        }



}

    public static class UnSafeArrayEx
    {
        public static unsafe UnSafeArray<T1> CreateUnSafeArray<T1>(in ReadOnlySpan<T1> array) where T1 : unmanaged
        {
            var p = (T1*)Marshal.AllocHGlobal(sizeof(T1) * array.Length);
            var newSpan = new Span<T1>(p, array.Length);
            array.CopyTo(newSpan);
            return new UnSafeArray<T1>(p, (uint)array.Length);
        }

        public static unsafe UnSafeArray<T1> CreateUnSafeArray<T1>(in Span<T1> array) where T1 : unmanaged
        {
            var p = (T1*)Marshal.AllocHGlobal(sizeof(T1) * array.Length);
            var newSpan = new Span<T1>(p, array.Length);
            array.CopyTo(newSpan);
            return new UnSafeArray<T1>(p, (uint)array.Length);
        }


        public static unsafe UnSafeArray<T1> CreateUnSafeArray<T1>(T1[] array) where T1 : unmanaged
        {
            var p = (T1*)Marshal.AllocHGlobal(sizeof(T1) * array.Length);
            fixed (T1* src = array)
            {
                Buffer.MemoryCopy(src, p, sizeof(T1) * array.Length, sizeof(T1) * array.Length);
            }
            return new UnSafeArray<T1>(p, (uint)array.Length);
        }
        public static unsafe UnSafeArray<T1> CreateUnSafeArray<T1>(in ArraySegment<T1> array) where T1 : unmanaged
        {
            var p = (T1*)Marshal.AllocHGlobal(sizeof(T1) * array.Array.Length);
            fixed (T1* src = array.Array)
            {
                Buffer.MemoryCopy(src, p, sizeof(T1) * array.Array.Length, sizeof(T1) * array.Array.Length);
            }
            return new UnSafeArray<T1>(p, (uint)array.Array.Length);
        }

    }
}
