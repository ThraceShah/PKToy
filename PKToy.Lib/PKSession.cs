
using System.Numerics;
using Viewer.IContract;

namespace PKToy.Lib;

public unsafe class PKSession
{
    public static void Init()
    {
        Frustrum.InitializeParasolidFrustrum();
    }

    public static void OpenPart(string partName, out AsmGeometry geometry)
    {
        PKErrorCheck err;
        PK.PART.receive_o_t receive_options = new(true);
        if(Path.GetExtension(partName) == ".x_t")
        {
            receive_options.transmit_format = PK.transmit_format_t.text_c;
        }
        else if (Path.GetExtension(partName) == ".x_b")
        {
            receive_options.transmit_format = PK.transmit_format_t.binary_c;
        }
        else
        {
            throw new NotSupportedException("Unsupported file format");
        }
        using var parts = new PKScopeArray<PK.PART_t>();
        err = PK.PART.receive(partName, &receive_options, &parts.size, &parts.data);
        var partitions = new PKScopeArray<PK.PARTITION_t>();
        err = PK.SESSION.ask_partitions(&partitions.size, &partitions.data);
        // PK.PARTITION_t partition;
        // err =PK.SESSION.ask_curr_partition(&partition);
        using var bodies = new PKScopeArray<PK.BODY_t>();
        err =PK.PARTITION.ask_bodies(partitions[0], &bodies.size, &bodies.data);
        using var goCallback=new PKGoCallback();
        Console.WriteLine("render faces");
        PK.TOPOL.render_facet_o_t facet_options = new(true);
        facet_options.go_option.go_edges = facet_go_edges_t.yes_c;
        facet_options.go_option.go_strips = facet_go_strips_t.yes_c;
        facet_options.go_option.go_max_facets_per_strip=65535;
        err = PK.TOPOL.render_facet(bodies.size, (TOPOL_t*)bodies.data, null, 0, &facet_options);
        var partGeometries=new PartGeometry[bodies.size];
        for (int i = 0; i < bodies.size; i++)
        {
            var body = bodies[i];
            partGeometries[i]=goCallback.GetPartGeometry(body);
        }
        var compGeometries=new CompGeometry[bodies.size];
        for (uint i = 0; i < bodies.size; i++)
        {
            compGeometries[i]=new ()
            {
                PartIndex=i,
                CompMatrix=Matrix4x4.Identity,
            };
        }
        geometry = new AsmGeometry(partGeometries,compGeometries);
    }
}