using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SourceSerializer
{
    public static class Serializer
    {

        public static void WriteArray<T>(this Stream stream, T[] collection)
        where T : unmanaged
        {
            unsafe
            {
                var l = collection.Length * sizeof(T);
                var lSpan = new Span<byte>(&l, sizeof(int));
                stream.Write(lSpan.ToArray(), 0, lSpan.Length);
                var cSpan = MemoryMarshal.Cast<T, byte>(collection);
                stream.Write(cSpan.ToArray(),0,cSpan.Length);
            }
        }


        public static T[] ReadArray<T>(this Stream stream)
        where T : unmanaged
        {
            var length = stream.ReadT<int>();
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            var cSpan = MemoryMarshal.Cast<byte, T>(buffer);
            return cSpan.ToArray();
        }


        public static void WriteT<T>(this Stream stream, T t)
        where T : unmanaged
        {
            unsafe
            {
                var cSpan = new Span<byte>(&t, sizeof(T));
                stream.Write(cSpan.ToArray(), 0, cSpan.Length);
            }
        }


        public static T ReadT<T>(this Stream stream)
        where T : unmanaged
        {
            unsafe
            {
                var byteLength = sizeof(T);
                var buffer = new byte[byteLength];
                stream.Read(buffer,0,byteLength);
                var cSpan = MemoryMarshal.Cast<byte, T>(buffer);
                return cSpan[0];
            }
        }

    }
}
