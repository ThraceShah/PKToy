using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PKToy;

public static class Frustrum
{
    const int FR_no_errors = 0;
    const int FR_unspecified = 99;
    const int FR_memory_full = 15;
    const int FR_end_of_file = 4;
    const int FR_close_fail = 14;

    static int frustrum_starts = 0;
    static int num_open_files = 0;

    const string end_of_header_s = "**END_OF_HEADER";

    class PSFile : IDisposable
    {
        private StreamReader file;
        private string data;

        public PSFile(string name)
        {
            file = new StreamReader(name);
            data = "";
        }

        public void SkipHeader()
        {
            SkipHeaderFile();
        }

        public unsafe void Read(int max, byte* buffer, int* n_read, int* ifail)
        {
            ReadFile(max, buffer, n_read, ifail);
        }

        private void SkipHeaderFile()
        {
            string? line;
            while ((line = file.ReadLine()) != null && !line.StartsWith(end_of_header_s))
            {

            }
        }


        private unsafe void ReadFile(int max, byte* buffer, int* n_read, int* ifail)
        {
            *ifail = FR_no_errors;
            *n_read = 0;
            var line = file.ReadLine();
            if (line == null)
            {
                *ifail = FR_end_of_file;
            }
            else
            {
                fixed (char* p = line)
                {
                    *n_read = Encoding.ASCII.GetBytes(p, line.Length, buffer, max);
                }
                //buffer[*n_read] = (byte)'\n';
                //*n_read += 1;
            }
        }

        public void Dispose()
        {
            file.Dispose();
        }
    }

    static Dictionary<int, PSFile> open_files = new Dictionary<int, PSFile>();
    static int next_file_id = 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FSTART(int* ifail)
    {
        *ifail = FR_unspecified;
        if (frustrum_starts == 0)
        {
            // Setup any data structures
        }
        ++frustrum_starts;
        *ifail = FR_no_errors;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FSTOP(int* ifail)
    {
        *ifail = FR_unspecified;
        if (frustrum_starts <= 0) return;
        --frustrum_starts;
        if (frustrum_starts == 0)
        {
            next_file_id = 0;
            open_files.Clear();
        }
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FMALLO(int* nbytes, byte** memory, int* ifail)
    {
        *ifail = FR_unspecified;
        if (frustrum_starts <= 0)
        {
            *memory = null;
            return;
        }
        *memory = (byte*)NativeMemory.Alloc((nuint)(*nbytes));
        if (*memory == null)
        {
            *ifail = FR_memory_full;
            return;
        }
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FMFREE(int* nbytes, byte** memory, int* ifail)
    {
        *ifail = FR_unspecified;
        if (frustrum_starts <= 0)
        {
            return;
        }
        NativeMemory.Free(*memory);
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFOPRD(int* guise, int* format, byte* name, int* namlen, int* skiphd, int* strid, int* ifail)
    {
        var name_str = Marshal.PtrToStringAnsi((nint)name, *namlen);
        *ifail = FR_unspecified;
        *strid = -1;
        if (frustrum_starts <= 0) return;
        var file = new PSFile(name_str);
        open_files[next_file_id] = file;
        if (*skiphd != 0)
        {
            open_files[next_file_id].SkipHeader();
        }
        *strid = next_file_id;
        ++next_file_id;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFOPWR(int* guise, int* format, byte* name, int* namlen, byte* pd2hdr, int* pd2len, int* strid, int* ifail)
    {
        // Dummy Function - we don't ever write
        *strid = 1;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFWRIT(int* guise, int* strid, int* nchars, byte* buffer, int* ifail)
    {
        // Dummy Function - we don't ever write
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFREAD(int* guise, int* strid, int* nmax, byte* buffer, int* nactual, int* ifail)
    {
        *ifail = FR_unspecified;
        *nactual = 0;
        if (frustrum_starts <= 0)
        {
            return;
        }
        if (open_files.TryGetValue(*strid, out var file))
        {
            *ifail = FR_no_errors;
            file.Read(*nmax, buffer, nactual, ifail);
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFCLOS(int* guise, int* strid, int* action, int* ifail)
    {
        *ifail = FR_unspecified;
        if (frustrum_starts <= 0)
        {
            return;
        }
        if (open_files.Remove(*strid,out var file))
        {
            file.Dispose();
            *ifail = FR_no_errors;
        }
        else
        {
            *ifail = FR_close_fail;
        }
    }

    private static void PrintMethodName([CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"Method: {methodName}");
    }
}