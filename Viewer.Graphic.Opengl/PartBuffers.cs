using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;
using Viewer.IContract;

namespace Viewer.Graphic.Opengl
{
    public class PartBuffers:IDisposable
    {
        private GL gl;

        private uint[] vaos;
        private uint[] vbos;
        private uint[] nbos;
        private uint[] cbos;
        private uint[] ebos;

        public uint Length{ get; private set; }

        private PartBuffers()
        {

        }

        public bool GetPartBuffer(uint partIndex,out uint vao,out uint ebo)
        {
            if(partIndex<Length)
            {
                vao = vaos[partIndex];
                ebo = ebos[partIndex];
                return true;
            }
            vao = 0;
            ebo = 0;
            return false;
        }


        public static unsafe PartBuffers GenPartBuffers(GL gl,in AsmGeometry asm)
        {
            var parts = asm.Parts;
            Span<uint> vaos = stackalloc uint[(int)parts.Length];
            gl.GenVertexArrays(vaos);
            gl.GenVertexArrays(vaos);
            Span<uint> vbos = stackalloc uint[(int)parts.Length];
            gl.GenBuffers(vbos);
            Span<uint> nbos = stackalloc uint[(int)parts.Length];
            gl.GenBuffers(nbos);
            Span<uint> cbos = stackalloc uint[(int)parts.Length];
            gl.GenBuffers(cbos);
            Span<uint> ebos = stackalloc uint[(int)parts.Length];
            gl.GenBuffers(ebos);
            for (int i = 0;i<parts.Length;i++)
            {
                gl.BindVertexArray(vaos[i]);

                gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);
                var vertices = parts[(uint)i].VertexArray;
                gl.BufferData(GLEnum.ArrayBuffer, sizeof(float)*4*vertices.Length, vertices.Ptr, GLEnum.StaticDraw);
                unsafe
                {
                    // note that we update the lamp's position attribute's stride to reflect the updated buffer data
                    gl.VertexAttribPointer(0, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(0);

                }

                gl.BindBuffer(GLEnum.ArrayBuffer, nbos[i]);
                var normals = parts[(uint)i].NormalArray;
                gl.BufferData(GLEnum.ArrayBuffer, sizeof(float)*3*normals.Length, normals.Ptr, GLEnum.StaticDraw);
                unsafe
                {
                    // Configure vertex attribute pointer for normals
                    gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(1);

                }

                gl.BindBuffer(GLEnum.ArrayBuffer, cbos[i]);
                var colors = parts[(uint)i].ColorArray;
                gl.BufferData(GLEnum.ArrayBuffer, sizeof(float) * 4 * colors.Length, colors.Ptr, GLEnum.StaticDraw);
                unsafe
                {
                    // Configure vertex attribute pointer for normals
                    gl.VertexAttribPointer(2, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                    gl.EnableVertexAttribArray(2);

                }

                gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                var indexSpan = parts[(uint)i].IndexArray;
                gl.BufferData(GLEnum.ElementArrayBuffer, sizeof(float) * indexSpan.Length,indexSpan.Ptr, GLEnum.StaticDraw);
            }
            // Unbind VAO to prevent accidental modification
            gl.BindVertexArray(0);
            var partBuffers = new PartBuffers
            {
                gl = gl,
                vaos=vaos.ToArray(),
                vbos=vbos.ToArray(),
                nbos=nbos.ToArray(),
                cbos=cbos.ToArray(),
                ebos=ebos.ToArray(),
                Length=parts.Length,
            };
            return partBuffers;
        }

        public void Dispose()
        {
            gl.DeleteVertexArrays(vaos);
            gl.DeleteBuffers(this.vbos);
            gl.DeleteBuffers(this.nbos);
            gl.DeleteBuffers(this.ebos);
            vaos = null;
            vbos = null;
            ebos = null;
            GC.SuppressFinalize(this);
        }
    }
}
