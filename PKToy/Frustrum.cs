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

    static string end_of_header_s = "**END_OF_HEADER";
    // static byte[] end_of_header = Encoding.ASCII.GetBytes("**END_OF_HEADER");

    static byte newline_c = (byte)'\n';

    class PSFile
    {
        private bool is_text;
        private FileStream file;
        private string data;
        private int loc;

        public PSFile(string name, int namelen)
        {
            if (namelen > 2 && name[0] == '*' && name[1] == '*')
            {
                data = name;
                loc = 0;
                is_text = true;
            }
            else
            {
                file = new FileStream(name, FileMode.Open, FileAccess.Read);
                is_text = false;
            }
        }

        public void SkipHeader()
        {
            if (is_text)
            {
                SkipHeaderText();
            }
            else
            {
                SkipHeaderFile();
            }
        }

        public unsafe void Read(int max, byte* buffer, int* n_read, int* ifail)
        {
            if (is_text)
            {
                ReadText(max, buffer, n_read, ifail);
            }
            else
            {
                ReadFile(max, buffer, n_read, ifail);
            }
        }

        private void SkipHeaderText()
        {
            // int end_of_header_start = data.IndexOf(new string(end_of_header));
            int end_of_header_start = data.IndexOf(end_of_header_s);
            loc = data.IndexOf('\n', end_of_header_start);
        }

        private void SkipHeaderFile()
        {
            PrintMethodName();
            using StreamReader reader = new(file, leaveOpen: true);
            string? line;
            while ((line = reader.ReadLine()) != null && !line.StartsWith(end_of_header_s))
            {

            }
            Console.Write(line);
            //file.Seek(0, SeekOrigin.Begin); // Reset the file stream position
        }

        private unsafe void ReadText(int max, byte* buffer, int* n_read, int* ifail)
        {
            *ifail = FR_no_errors;
            if (loc >= data.Length)
            {
                *ifail = FR_end_of_file;
            }
            if (loc + max > data.Length)
            {
                *n_read = data.Length - loc;
            }
            else
            {
                *n_read = max;
            }
            string to_return = data.Substring(loc, *n_read);
            loc += *n_read;
            byte[] bytes = Encoding.ASCII.GetBytes(to_return);
            for (int i = 0; i < *n_read; i++)
            {
                buffer[i] = bytes[i];
            }
        }

        private unsafe void ReadFile(int max, byte* buffer, int* n_read, int* ifail)
        {
            *ifail = FR_no_errors;
            *n_read = 0;
            Span<byte> b = stackalloc byte[1];
            for (int i = 0; i < max; ++i)
            {
                int byteRead = file.Read(b);
                if (byteRead > 0)
                {
                    buffer[i] = b[0];
                    ++(*n_read);
                    if (buffer[i] == newline_c) break;
                }
                else
                {
                    if (*n_read < max)
                    {
                        *ifail = FR_end_of_file;
                    }
                    break;
                }
            }
        }
    }

    static Dictionary<int, PSFile> open_files = new Dictionary<int, PSFile>();
    static int next_file_id = 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FSTART(int* ifail)
    {
        PrintMethodName();
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
        PrintMethodName();
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
        PrintMethodName();
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
        PrintMethodName();
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
        PrintMethodName();
        var name_str = Marshal.PtrToStringAnsi((nint)name, *namlen);
        *ifail = FR_unspecified;
        *strid = -1;
        if (frustrum_starts <= 0) return;
        var file = new PSFile(name_str, *namlen);
        //open_files.Add(next_file_id, file);
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
        PrintMethodName();
        // Dummy Function - we don't ever write
        *strid = 1;
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFWRIT(int* guise, int* strid, int* nchars, byte* buffer, int* ifail)
    {
        PrintMethodName();
        // Dummy Function - we don't ever write
        *ifail = FR_no_errors;
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFREAD(int* guise, int* strid, int* nmax, byte* buffer, int* nactual, int* ifail)
    {
        PrintMethodName();
        *ifail = FR_unspecified;
        *nactual = 0;
        if (frustrum_starts <= 0) return;
        if (open_files.TryGetValue(*strid, out var file))
        {
            *ifail = FR_no_errors;
            file.Read(*nmax, buffer, nactual, ifail);
            var str = Encoding.ASCII.GetString(buffer, *nactual);
            Console.Write(str);
            if (*ifail != FR_no_errors)
            {
            }
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void FFCLOS(int* guise, int* strid, int* action, int* ifail)
    {
        PrintMethodName();
        *ifail = FR_unspecified;
        if (frustrum_starts <= 0) return;
        if (open_files.Remove(*strid))
        {
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