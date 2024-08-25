
namespace PKToy.Lib;


internal unsafe class Program
{
    private static void Main(string[] args)
    {
        Frustrum.InitializeParasolidFrustrum();
        PKErrorCheck err;
        PK.PART.receive_o_t receive_options = new(true);
        receive_options.transmit_format = PK.transmit_format_t.text_c;
        string part_name = "D:\\model\\2cube.x_t";
        using var parts = new PKScopeArray<PK.PART_t>();
        err = PK.PART.receive(part_name, &receive_options, &parts.size, &parts.data);

        Console.Write($"part nums:{parts.size}, part tags:");
        foreach (var part in parts.AsSpan)
        {
            Console.Write($"{part.Value}, ");
        }
        Console.WriteLine();

        var partitions = new PKScopeArray<PK.PARTITION_t>();
        err = PK.SESSION.ask_partitions(&partitions.size, &partitions.data);
        Console.WriteLine($"partitions num:{partitions.size}");
        var bodies = new PKScopeArray<PK.BODY_t>();
        err = PK.PARTITION.ask_bodies(partitions.data[0], &bodies.size, &bodies.data);
        Console.WriteLine($"bodies num:{bodies.size}, body tags:");
        PK.BODY.ask_parent_o_t ask_parent_options = new(true);
        foreach (var body in bodies.AsSpan)
        {
            Console.Write($"{body.Value}, ");
        }
        Console.WriteLine();
        PK.TOPOL.facet_o_t facet_options = new(true);
        PK.TOPOL.facet_r_t facet_result;
        err = PK.TOPOL.facet(bodies.size, (TOPOL_t*)bodies.data, null, 0, &facet_options, &facet_result);
        err = PK.TOPOL.facet_r_f(&facet_result);

        
    }


}