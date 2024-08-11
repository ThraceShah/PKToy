
namespace PKToy;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static PKToy.Frustrum;
internal unsafe partial class Program
{
    private static void Main(string[] args)
    {
        initialize_parasolid_frustrum();
        PK.ERROR.code_t err;
        PK.PART.receive_o_t receive_options = new(true);
        receive_options.transmit_format = PK.transmit_format_t.text_c;
        string part_name = "D:\\model\\cube.x_t";
        int partNum;
        PK.PART_t* parts;
        err = PK.PART.receive(part_name, &receive_options, &partNum, &parts);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("msvcrt.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void* malloc(ulong size);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("msvcrt.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void free(void* ptr);

    static void initialize_parasolid_frustrum()
    {
        PK.SESSION.frustrum_v fru = new()
        {
            fstart = &FSTART,
            fstop = &FSTOP,
            fmallo = &FMALLO,
            fmfree = &FMFREE,
            ffoprd = &FFOPRD,
            ffopwr = &FFOPWR,
            ffclos = &FFCLOS,
            ffread = &FFREAD,
            ffwrit = &FFWRIT
        };
        PK.SESSION._register_frustrum(&fru);
        PK.MEMORY.frustrum_t a = new()
        {
            alloc_fn = &malloc,
            free_fn = &free
        };
        PK.MEMORY.register_callbacks(a);
        PK.ERROR.code_t err;
        PK.SESSION.start_o_t start_options = new(true);
        err = PK.SESSION.start(&start_options);
        err = PK.SESSION.set_unicode(PK.LOGICAL_t.@true);

    }


}