using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NativeCorLib;
using Silk.NET.OpenGL;
using Viewer.IContract;

namespace Viewer.Graphic.Opengl
{
    public class PartBuffers : IDisposable
    {
        private GL gl;

        private uint[] vaos;
        private uint[] vbos;
        private uint[] nbos;
        private uint[] cbos;
        private uint[] ebos;
        private uint[] ibos;

        public int Length { get; private set; }

        private PartBuffers()
        {

        }

        public bool GetPartBuffer(int partIndex, out uint vao, out uint ebo)
        {
            if (partIndex < Length)
            {
                vao = vaos[partIndex];
                ebo = ebos[partIndex];
                return true;
            }
            vao = 0;
            ebo = 0;
            return false;
        }


        public static unsafe PartBuffers GenPartBuffers(GL gl, in AsmGeometry asm)
        {
            var parts = asm.Parts;
            var vaos = new uint[parts.Count];
            gl.GenVertexArrays(vaos);
            gl.GenVertexArrays(vaos);
            var vbos = new uint[parts.Count];
            gl.GenBuffers(vbos);
            var nbos = new uint[parts.Count];
            gl.GenBuffers(nbos);
            var cbos = new uint[parts.Count];
            gl.GenBuffers(cbos);
            var ebos = new uint[parts.Count];
            gl.GenBuffers(ebos);
            var ibos = new uint[parts.Count];
            gl.GenBuffers(ibos);
            var memoryPoolCapacity = parts.Max(p => p.OutPutSize);
            nint arrayPool = (nint)NativeMemory.Alloc((nuint)memoryPoolCapacity);
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is StripFacePart stripFace)
                {
                    var partOut = stripFace.Update(arrayPool);
                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);
                    gl.BufferData(GLEnum.ArrayBuffer, partOut.Points.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(0);

                    gl.BindBuffer(GLEnum.ArrayBuffer, ibos[i]);
                    gl.BufferData(GLEnum.ArrayBuffer, partOut.IndicesCells.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(1, 1, GLEnum.Float, false, sizeof(float), 0);
                    gl.EnableVertexAttribArray(1);

                    gl.BindBuffer(GLEnum.ArrayBuffer, nbos[i]);
                    gl.BufferData(GLEnum.ArrayBuffer, partOut.Normals.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(2, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(2);

                    gl.BindBuffer(GLEnum.ArrayBuffer, cbos[i]);
                    gl.BufferData(GLEnum.ArrayBuffer, partOut.Colors.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(3, 1, GLEnum.Float, false, sizeof(float), 0);
                    gl.EnableVertexAttribArray(3);

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    gl.BufferData(GLEnum.ElementArrayBuffer, partOut.Indices.ReadOnlySpan, GLEnum.StaticDraw);
                }
                else if (parts[i] is EdgePart edgePart)
                {
                    var partOut = edgePart.Update(arrayPool);

                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);

                    gl.BufferData(GLEnum.ArrayBuffer, partOut.Points.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(0);

                    gl.BindBuffer(GLEnum.ArrayBuffer, ibos[i]);
                    gl.BufferData(GLEnum.ArrayBuffer, partOut.IndicesCells.ReadOnlySpan, GLEnum.StaticDraw);
                    gl.VertexAttribPointer(1, 1, GLEnum.Float, false, sizeof(float), 0);
                    gl.EnableVertexAttribArray(1);

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    gl.BufferData(GLEnum.ElementArrayBuffer, partOut.Indices.ReadOnlySpan, GLEnum.StaticDraw);
                }

            }

            NativeMemory.Free((void*)arrayPool);
            // Unbind VAO to prevent accidental modification
            gl.BindVertexArray(0);
            var partBuffers = new PartBuffers
            {
                gl = gl,
                vaos = vaos,
                vbos = vbos,
                nbos = nbos,
                cbos = cbos,
                ebos = ebos,
                ibos = ibos,
                Length = parts.Count,
            };
            return partBuffers;
        }

        public void Dispose()
        {
            gl.DeleteVertexArrays(vaos);
            gl.DeleteBuffers(this.vbos);
            gl.DeleteBuffers(this.nbos);
            gl.DeleteBuffers(this.cbos);
            gl.DeleteBuffers(this.ebos);
            gl.DeleteBuffers(this.ibos);
            vaos = null;
            vbos = null;
            nbos = null;
            cbos = null;
            ebos = null;
            ibos = null;
            GC.SuppressFinalize(this);
        }
    }
}
