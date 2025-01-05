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
    public class PartBuffers : IDisposable
    {
        private GL gl;

        private uint[] vaos;
        private uint[] vbos;
        private uint[] nbos;
        private uint[] cbos;
        private uint[] ebos;

        public int Length { get; private set; }

        private PartBuffers()
        {

        }

        public bool GetPartBuffer(uint partIndex, out uint vao, out uint ebo)
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
            Span<uint> vaos = stackalloc uint[parts.Count];
            gl.GenVertexArrays(vaos);
            gl.GenVertexArrays(vaos);
            Span<uint> vbos = stackalloc uint[parts.Count];
            gl.GenBuffers(vbos);
            Span<uint> nbos = stackalloc uint[parts.Count];
            gl.GenBuffers(nbos);
            Span<uint> cbos = stackalloc uint[parts.Count];
            gl.GenBuffers(cbos);
            Span<uint> ebos = stackalloc uint[parts.Count];
            gl.GenBuffers(ebos);
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] is StripFacePart stripFace)
                {
                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);
                    using var vertices = stripFace.GetPoints();
                    // ReadOnlySpan<Vector4> vertices = shadedPart.PointsSpan;
                    gl.BufferData(GLEnum.ArrayBuffer, vertices.ReadOnlySpan, GLEnum.StaticDraw);
                    unsafe
                    {
                        // note that we update the lamp's position attribute's stride to reflect the updated buffer data
                        gl.VertexAttribPointer(0, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(0);

                    }

                    gl.BindBuffer(GLEnum.ArrayBuffer, nbos[i]);
                    //ReadOnlySpan<Vector3> normals = shadedPart.NormalsSpan;
                    using var normals = stripFace.GetNormals();
                    gl.BufferData(GLEnum.ArrayBuffer, normals.ReadOnlySpan, GLEnum.StaticDraw);
                    unsafe
                    {
                        // Configure vertex attribute pointer for normals
                        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(1);

                    }

                    gl.BindBuffer(GLEnum.ArrayBuffer, cbos[i]);
                    // ReadOnlySpan<uint> colors = shadedPart.ColorsSpan;
                    using var colors = stripFace.GetColors();
                    gl.BufferData(GLEnum.ArrayBuffer, colors.ReadOnlySpan, GLEnum.StaticDraw);
                    unsafe
                    {
                        // Configure vertex attribute pointer for normals
                        gl.VertexAttribPointer(2, 1, GLEnum.UnsignedInt, false, sizeof(uint), 0);
                        gl.EnableVertexAttribArray(2);

                    }

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    // ReadOnlySpan<int> indexSpan = shadedPart.IndicesSpan;
                    using var indices = stripFace.GetIndices();
                    gl.BufferData(GLEnum.ElementArrayBuffer, indices.ReadOnlySpan, GLEnum.StaticDraw);
                }
                else if (parts[i] is StripFaceGeometry shadedPart)
                {
                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);
                    ReadOnlySpan<Vector4> vertices = shadedPart.PointsSpan;
                    gl.BufferData(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
                    unsafe
                    {
                        // note that we update the lamp's position attribute's stride to reflect the updated buffer data
                        gl.VertexAttribPointer(0, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(0);

                    }

                    gl.BindBuffer(GLEnum.ArrayBuffer, nbos[i]);
                    ReadOnlySpan<Vector3> normals = shadedPart.NormalsSpan;
                    gl.BufferData(GLEnum.ArrayBuffer, normals, GLEnum.StaticDraw);
                    unsafe
                    {
                        // Configure vertex attribute pointer for normals
                        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 3 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(1);

                    }

                    gl.BindBuffer(GLEnum.ArrayBuffer, cbos[i]);
                    ReadOnlySpan<uint> colors = shadedPart.ColorsSpan;
                    gl.BufferData(GLEnum.ArrayBuffer, colors, GLEnum.StaticDraw);
                    unsafe
                    {
                        // Configure vertex attribute pointer for normals
                        gl.VertexAttribPointer(2, 1, GLEnum.UnsignedInt, false, sizeof(uint), 0);
                        gl.EnableVertexAttribArray(2);

                    }

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    ReadOnlySpan<int> indexSpan = shadedPart.IndicesSpan;
                    gl.BufferData(GLEnum.ElementArrayBuffer, indexSpan, GLEnum.StaticDraw);
                }
                else if (parts[i] is EdgeGeometry wireframePart)
                {
                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);
                    ReadOnlySpan<Vector4> vertices = wireframePart.PointsSpan;
                    gl.BufferData(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
                    unsafe
                    {
                        // note that we update the lamp's position attribute's stride to reflect the updated buffer data
                        gl.VertexAttribPointer(0, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(0);

                    }

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    ReadOnlySpan<int> indexSpan = wireframePart.IndicesSpan;
                    gl.BufferData(GLEnum.ElementArrayBuffer, indexSpan, GLEnum.StaticDraw);
                }
                else if (parts[i] is EdgePart edgePart)
                {
                    gl.BindVertexArray(vaos[i]);

                    gl.BindBuffer(GLEnum.ArrayBuffer, vbos[i]);

                    using var vertices = edgePart.GetPoints();
                    gl.BufferData(GLEnum.ArrayBuffer, vertices.ReadOnlySpan, GLEnum.StaticDraw);
                    unsafe
                    {
                        // note that we update the lamp's position attribute's stride to reflect the updated buffer data
                        gl.VertexAttribPointer(0, 4, GLEnum.Float, false, 4 * sizeof(float), 0);
                        gl.EnableVertexAttribArray(0);

                    }

                    gl.BindBuffer(GLEnum.ElementArrayBuffer, ebos[i]);
                    using var indices = edgePart.GetIndices();
                    gl.BufferData(GLEnum.ElementArrayBuffer, indices.ReadOnlySpan, GLEnum.StaticDraw);
                }

            }
            // Unbind VAO to prevent accidental modification
            gl.BindVertexArray(0);
            var partBuffers = new PartBuffers
            {
                gl = gl,
                vaos = vaos.ToArray(),
                vbos = vbos.ToArray(),
                nbos = nbos.ToArray(),
                cbos = cbos.ToArray(),
                ebos = ebos.ToArray(),
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
            vaos = null;
            vbos = null;
            nbos = null;
            cbos = null;
            ebos = null;
            GC.SuppressFinalize(this);
        }
    }
}
