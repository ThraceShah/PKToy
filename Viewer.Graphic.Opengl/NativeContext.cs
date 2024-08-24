using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;

namespace Viewer.Graphic.Opengl;

public struct NativeContext : INativeContext
{
    private delegate nint Apifunc(string name);

    private readonly Apifunc _getProcAddress;

    public NativeContext(nint ptr)
    {
        _getProcAddress = Marshal.GetDelegateForFunctionPointer<Apifunc>(ptr);
    }

    public nint GetProcAddress(string proc, int? slot = null)
    {
        IntPtr intPtr = _getProcAddress(proc);
        if (intPtr == (IntPtr)0)
        {
            Throw(proc);
        }

        return intPtr;
        static void Throw(string proc)
        {
            throw new SymbolLoadingException(proc);
        }
    }

    public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
    {
        return (addr = _getProcAddress(proc)) != 0;
    }

    public void Dispose()
    {

    }
}
