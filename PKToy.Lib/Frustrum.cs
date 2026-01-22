using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PKToy.Lib;
public unsafe static class Frustrum
{
    const string end_of_header_s = "**END_OF_HEADER";

    class PSFile : IDisposable
    {
        private readonly FileStream fileStream;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        public PSFile(string name)
        {
            fileStream = new(name, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            reader = new(fileStream, Encoding.ASCII);
            writer = new(fileStream, Encoding.ASCII);
        }
        public void SkipHeader()
        {
            Span<byte> buffer1 = stackalloc byte[2];
            reader.Read(buffer1);
            if (buffer1[0] == 0x2A && buffer1[1] == 0x2A)
            {
                var sb = new StringBuilder();
                Span<byte> buffer = stackalloc byte[1];
                while (reader.Read(buffer) > 0)
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
            else
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
        }


        public unsafe void Read(int max, byte* buffer, int* n_read, int* ifail)
        {
            *ifail = FR_no_errors;
            *n_read = 0;
            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                *ifail = FR_end_of_file;
                return;
            }
            var span = new Span<byte>(buffer, max);
            *n_read = reader.Read(span);
        }

        public unsafe void Write(int nchars, byte* buffer, int* ifail)
        {
            *ifail = FR_no_errors;
            writer.Write(new Span<byte>(buffer, nchars));
        }

        public void Dispose()
        {
            reader.Dispose();
            writer.Dispose();
            fileStream.Dispose();
        }
    }

    static Dictionary<int, PSFile> open_files = [];
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
            case FFCSCH:
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
        if (*skiphd == FFSKHD)
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
        var name_str = new string((sbyte*)name, 0, *namlen, Encoding.UTF8);
        var file = new PSFile(name_str);
        open_files[next_file_id] = file;
        *strid = next_file_id;
        ++next_file_id;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FrustrumFileWrite(int* guise, int* strid, int* nchars, byte* buffer, int* ifail)
    {
        *ifail = FR_unspecified;
        if (open_files.TryGetValue(*strid, out var file))
        {
            *ifail = FR_no_errors;
            file.Write(*nchars, buffer, ifail);
        }
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe PK_ERROR_code_t ErrorHandler(PK_ERROR_sf_t* error)
    {
        string? functionName = Marshal.PtrToStringAnsi((nint)error->function);
        string? errCodeName = Marshal.PtrToStringAnsi((nint)error->code_token);
        Console.WriteLine($"Error in {functionName} with code {errCodeName},err arg number:{error->argument_number},err arg index:{error->argument_index},err entity:{error->entity}");
        return error->code;
    }



    public static unsafe void InitializeParasolidFrustrum()
    {
#if WINDOWS
        string libc = "msvcrt";
#elif UNIX
        string libc = "libc";
#endif
        var handle = NativeLibrary.Load(libc);
        var mallocPtr = (delegate* unmanaged[Cdecl]<ulong, void*>)NativeLibrary.GetExport(handle, "malloc");
        var freePtr = (delegate* unmanaged[Cdecl]<void*, void>)NativeLibrary.GetExport(handle, "free");
        PKErrorCheck err;
        PK_SESSION_frustrum_t fru = new()
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
        err = PK_SESSION_register_frustrum(&fru);
        PK_MEMORY_frustrum_t a = new()
        {
            alloc_fn = mallocPtr,
            free_fn = freePtr
        };
        err = PK_MEMORY_register_callbacks(a);
        PK_ERROR_frustrum_t errFru = &ErrorHandler;
        err = PK_ERROR_register_callbacks(errFru);
    }

}