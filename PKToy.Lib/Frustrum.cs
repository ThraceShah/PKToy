using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PKToy.Lib;
using static PK.UNCLASSED;
public unsafe static class Frustrum
{
    const int FR_no_errors = (int)frustrum_ifails_t.FR_no_errors;
    const int FR_unspecified = (int)frustrum_ifails_t.FR_unspecified;
    const int FR_end_of_file = (int)frustrum_ifails_t.FR_end_of_file;
    const int FR_close_fail = (int)frustrum_ifails_t.FR_close_fail;
    const string end_of_header_s = "**END_OF_HEADER";

    class PSFile(string name) : IDisposable
    {
        private readonly BinaryReader file = new(new FileStream(name, FileMode.Open, FileAccess.Read), Encoding.ASCII);

        public void SkipHeader()
        {
            var sb = new StringBuilder();
            Span<byte> buffer = stackalloc byte[1];
            while (file.Read(buffer) > 0)
            {
                if (buffer[0] == (byte)'\n')
                {
                    var line = sb.ToString();
                    if (line.StartsWith(end_of_header_s))
                    {
                        break;
                    }
                    sb.Clear();
                }
                else
                {
                    sb.Append((char)buffer[0]);
                }
            }
        }

        public unsafe void Read(int max, byte* buffer, int* n_read, int* ifail)
        {
            *ifail = FR_no_errors;
            *n_read = 0;
            if (file.BaseStream.Position == file.BaseStream.Length)
            {
                *ifail = FR_end_of_file;
                return;
            }
            var span = new Span<byte>(buffer, max);
            *n_read = file.Read(span);
        }

        public void Dispose()
        {
            file.Dispose();
        }
    }

    static Dictionary<int, PSFile> open_files = new Dictionary<int, PSFile>();
    static int next_file_id = 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumStart(int* ifail)
    {
        *ifail = FR_no_errors;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumStop(int* ifail)
    {
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumMemoryAlloc(int* nbytes, byte** memory, int* ifail)
    {
        *memory = (byte*)NativeMemory.Alloc((nuint)(*nbytes));
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumMemoryFree(int* nbytes, byte** memory, int* ifail)
    {
        NativeMemory.Free(*memory);
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileOpenRead(int* guise, int* format, byte* name, int* namlen, int* skiphd, int* strid, int* ifail)
    {
        PrintParameters(guise, format, name, namlen, skiphd, strid, ifail);
        var name_str = new string((sbyte*)name, 0, *namlen, Encoding.UTF8);
        switch ((file_guise_tokens_t)(*guise))
        {
            case file_guise_tokens_t.FFCSCH:
            {
                name_str = Path.Combine(AppContext.BaseDirectory, $"pschema/{name_str}.s_t");
                break;
            }
            default:
            {
                break;
            }
        }
        *ifail = FR_unspecified;
        *strid = -1;
        var file = new PSFile(name_str);
        open_files[next_file_id] = file;
        if (*skiphd == (int)file_open_mode_tokens_t.FFSKHD)
        {
            open_files[next_file_id].SkipHeader();
        }
        *strid = next_file_id;
        ++next_file_id;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileOpenWrite(int* guise, int* format, byte* name, int* namlen, byte* pd2hdr, int* pd2len, int* strid, int* ifail)
    {
        PrintParameters(guise, format, name, namlen, pd2hdr, pd2len, strid, ifail);
        // Dummy Function - we don't ever write
        *strid = 1;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileWrite(int* guise, int* strid, int* nchars, byte* buffer, int* ifail)
    {
        // PrintParameters(guise, strid, nchars, buffer, ifail);
        // Dummy Function - we don't ever write
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileRead(int* guise, int* strid, int* nmax, byte* buffer, int* nactual, int* ifail)
    {
        // PrintParameters(guise, strid, nmax, buffer, nactual, ifail);
        *ifail = FR_unspecified;
        *nactual = 0;
        if (open_files.TryGetValue(*strid, out var file))
        {
            *ifail = FR_no_errors;
            file.Read(*nmax, buffer, nactual, ifail);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileClose(int* guise, int* strid, int* action, int* ifail)
    {
        PrintParameters(guise, strid, action, ifail);
        *ifail = FR_unspecified;
        if (open_files.Remove(*strid, out var file))
        {
            file.Dispose();
            *ifail = FR_no_errors;
        }
        else
        {
            *ifail = FR_close_fail;
        }
    }

    static PKGoCallback? goCallback;

    public static void RegGoCallback(PKGoCallback callback)
    {
        goCallback = callback;
    }

    public static void UnRegGoCallback()
    {
        goCallback = null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GOSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        goCallback?.GOSegment(segtyp, ntags, tags, ngeom, geom, nlntp, lntp, ifail);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GOOpenSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        
        goCallback?.GOOpenSegment(segtyp, ntags, tags, ngeom, geom, nlntp, lntp, ifail);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GOCloseSegment(int* segtyp, int* ntags, int* tags, int* ngeom, double* geom, int* nlntp, int* lntp, int* ifail)
    {
        goCallback?.GOCloseSegment(segtyp, ntags, tags, ngeom, geom, nlntp, lntp, ifail);
    }



    public static void InitializeParasolidFrustrum()
    {
#if WINDOWS
        string libc = "msvcrt";
#elif UNIX
        string libc = "libc";
#endif
        var handle = NativeLibrary.Load(libc);
        var mallocPtr = (delegate* unmanaged[Cdecl]<ulong, void*>)NativeLibrary.GetExport(handle, "malloc");
        var freePtr = (delegate* unmanaged[Cdecl]<void*, void>)NativeLibrary.GetExport(handle, "free");

        PK.SESSION.frustrum_v fru = new()
        {
            fstart = &FrustrumStart,
            fstop = &FrustrumStop,
            fmallo = &FrustrumMemoryAlloc,
            fmfree = &FrustrumMemoryFree,
            ffoprd = &FrustrumFileOpenRead,
            ffopwr = &FrustrumFileOpenWrite,
            ffclos = &FrustrumFileClose,
            ffread = &FrustrumFileRead,
            ffwrit = &FrustrumFileWrite,
            gosgmt = &GOSegment,
            goopsg = &GOOpenSegment,
            goclsg = &GOCloseSegment
        };
        PK.SESSION._register_frustrum(&fru);
        PK.MEMORY.frustrum_t a = new()
        {
            alloc_fn = mallocPtr,
            free_fn = freePtr
        };
        PK.MEMORY.register_callbacks(a);
        PK.ERROR.code_t err;
        PK.SESSION.start_o_t start_options = new(true);
        err = PK.SESSION.start(&start_options);
        err = PK.SESSION.set_unicode(PK.LOGICAL_t.@true);
        PK.SESSION.set_roll_forward(LOGICAL_t.@true);
    }

}