using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Viewer.IContract;

namespace Viewer.Graphic.Opengl
{
    public class UnmanagedCaller
    {
        private static readonly Dictionary<int, GlRender> _objectPool = new(5);

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "CreateGlRender")]
        public unsafe static int CreateGlRender(nint ptr)
        {
            GL gl = GL.GetApi(new NativeContext(ptr));
#else
        public unsafe static int CreateGlRender(Func<string, nint> func)
        {
            GL gl = GL.GetApi(func);
#endif
            int id = _objectPool.Count + 1;
            var obj = new GlRender(gl);
            _objectPool.Add(id, obj);
            return id;
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "ReleaseGlRender")]
#endif
        public static void ReleaseGlRender(int id)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.Dispose();
                _objectPool.Remove(id);
                Console.WriteLine($"id:{id},ReleaseGlRender");
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "GLControlLoad")]
#endif
        public static void GLControlLoad(int id)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.GLControlLoad();
                Console.WriteLine($"id:{id},GLControlLoad");
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "GLControlResize")]
#endif
        public static void GLControlResize(int id, uint width, uint height)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.GLControlResize(width, height);
                Console.WriteLine($"id:{id},GLControlResize");
            }
        }
#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "Render")]
#endif
        public static void Render(int id)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.Render();
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "UpdateGeometry")]
#endif
        public static void UpdateGeometry(int id,nint geometryPtr)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                AsmGeometry geometry;
                unsafe
                {
                    geometry = *(AsmGeometry*)geometryPtr;
                }
                render.UpdateGeometry(ref geometry);
                Console.WriteLine($"id:{id},UpdateGeometry");
            }

        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "MouseDown")]
#endif
        public static void MouseDown(int id,KeyCode keyCode,int x,int y)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.MouseDown(keyCode, x, y);
            }
        }


#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "MouseUp")]
#endif
        public static void MouseUp(int id, KeyCode keyCode, int x, int y)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.MouseUp(keyCode, x, y);
            }
        }


#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "MouseMove")]
#endif
        public static void MouseMove(int id, int x, int y)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.MouseMove(x, y);
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "MouseWheel")]
#endif
        public static void MouseWheel(int id, int delta)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.MouseWheel(delta);
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "KeyDown")]
#endif
        public static void KeyDown(int id, KeyCode keyCode)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.KeyDown(keyCode);
            }
        }

#if RELEASE
        [UnmanagedCallersOnly(EntryPoint = "KeyUp")]
#endif
        public static void KeyUp(int id, KeyCode keyCode)
        {
            if (_objectPool.TryGetValue(id, out var render))
            {
                render.KeyUp(keyCode);
            }
        }


    }
}
