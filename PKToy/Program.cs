
namespace PKToy;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PK;
using static PKToy.Frustrum;


public unsafe ref struct ScopeArray<T> where T : unmanaged
{
    public int Num;

    public T* PArray;

    public Span<T> AsSpan => new(PArray, Num);

    public void Dispose()
    {
        if (PArray != null)
        {
            var err = PK.MEMORY.free(PArray);
        }
        PArray = null;
    }
}


internal unsafe partial class Program
{
    private static void Main(string[] args)
    {
        initialize_parasolid_frustrum();
        PK.ERROR.code_t err;
        PK.PART.receive_o_t receive_options = new(true);
        receive_options.transmit_format = PK.transmit_format_t.text_c;
        string part_name = "D:\\model\\2cube.x_t";
        using var parts = new ScopeArray<PK.PART_t>();
        err = PK.PART.receive(part_name, &receive_options, &parts.Num, &parts.PArray);

        var partitions = new ScopeArray<PK.PARTITION_t>();
        err = PK.SESSION.ask_partitions(&partitions.Num, &partitions.PArray);
        Console.WriteLine($"partitions num:{partitions.Num}");
        var bodies = new ScopeArray<PK.BODY_t>();
        err = PK.PARTITION.ask_bodies(partitions.PArray[0], &bodies.Num, &bodies.PArray);
        Console.WriteLine($"bodies num:{bodies.Num}, body tags:");
        PK.BODY.ask_parent_o_t ask_parent_options = new(true);
        foreach (var body in bodies.AsSpan)
        {
            Console.Write($"{body.Value}, ");
        }
        PK.TOPOL.facet_o_t facet_options = new(true);
        PK.TOPOL.facet_r_t facet_result;
        err = PK.TOPOL.facet(bodies.Num, (TOPOL_t*)bodies.PArray, null, 0, &facet_options, &facet_result);
        err = PK.TOPOL.facet_r_f(&facet_result);
        Console.WriteLine();
        Console.Write($"part nums:{parts.Num}, part tags:");
        foreach (var part in parts.AsSpan)
        {
            Console.Write($"{part.Value}, ");
        }
        Console.WriteLine();
    }

    [LibraryImport("msvcrt.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void* malloc(ulong size);

    [LibraryImport("msvcrt.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe partial void free(void* ptr);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void* CMalloc(ulong size)
    {
        return malloc(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void CFree(void* ptr)
    {
        free(ptr);
    }


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
            alloc_fn = &CMalloc,
            free_fn = &CFree
        };
        PK.MEMORY.register_callbacks(a);
        PK.ERROR.code_t err;
        PK.SESSION.start_o_t start_options = new(true);
        err = PK.SESSION.start(&start_options);
        err = PK.SESSION.set_unicode(PK.LOGICAL_t.@true);

    }


}