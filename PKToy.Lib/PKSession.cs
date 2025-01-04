
using System.Diagnostics;
using System.Numerics;
using Viewer.IContract;

namespace PKToy.Lib;

public unsafe class PKSession
{
    public static void Init()
    {
        Frustrum.InitializeParasolidFrustrum();
        PK.SESSION.smp_o_t smpOptions = new(true);
        PK.SESSION.set_smp(&smpOptions);
        PK.SESSION.smp_r_t smpResult;
        PK.SESSION.ask_smp(&smpResult);
    }

    public static void CallRenderFacet(Span<PK.BODY_t> bodies)
    {
        var watch = new Stopwatch();
        watch.Start();
        PK.TOPOL.render_facet_o_t facetOptions = new(true);
        facetOptions.go_option.go_normals = facet_go_normals_t.yes_c;
        facetOptions.go_option.go_edges = facet_go_edges_t.yes_c;
        facetOptions.go_option.go_strips = facet_go_strips_t.yes_c;
        facetOptions.go_option.go_max_facets_per_strip = 65535;
        facetOptions.go_option.go_interleaved = facet_go_interleaved_t.yes_c;
        PK.TOPOL.render_facet(bodies.Length, (TOPOL_t*)Unsafe.AsPointer(ref bodies[0]), null, 0, &facetOptions);
        watch.Stop();
        Console.WriteLine($"render facet elapsed time:{watch.ElapsedMilliseconds} ms");
        return;
    }


    public static void OpenPart(string partName, out AsmGeometry geometry)
    {
        PK.POINT_t point;
        PK.VERTEX.ask_point(0, &point);
        PK.PART.receive_o_t receive_options = new(true);
        if (Path.GetExtension(partName).Equals(".x_t", StringComparison.OrdinalIgnoreCase))
        {
            receive_options.transmit_format = PK.transmit_format_t.text_c;
        }
        else if (Path.GetExtension(partName).Equals(".x_b", StringComparison.OrdinalIgnoreCase))
        {
            receive_options.transmit_format = PK.transmit_format_t.binary_c;
        }
        else
        {
            throw new NotSupportedException("Unsupported file format");
        }
        var watch = new Stopwatch();
        watch.Start();
        using var parts = new PKScopeArray<PK.PART_t>();
        PK.PART.receive(partName, &receive_options, &parts.size, &parts.data);
        PK.PARTITION_t partition;
        PK.SESSION.ask_curr_partition(&partition);
        using var bodies = new PKScopeArray<PK.BODY_t>();
        PK.PARTITION.ask_bodies(partition, &bodies.size, &bodies.data);
        watch.Stop();
        Console.WriteLine($"kernel load model elapsed time:{watch.ElapsedMilliseconds} ms");

        watch.Restart();
        using var goCallback = new PKGoCallback();
        Console.WriteLine("render faces");
        CallRenderFacet(bodies.Span);
        var bodiesSet = new HashSet<PK.BODY_t>();
        for (int i = 0; i < bodies.size; i++)
        {
            bodiesSet.Add(bodies[i]);
        }
        var asmGeom = new AsmGeometry();

        var identityTransform = new TRANSF_sf_t(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        TRANSF_t identityTrans;
        PK.TRANSF.create(&identityTransform, &identityTrans);
        INSTANCE_sf_t instanceSF;
        TRANSF_sf_t transformSF;
        CLASS_t classSF;
        using var assemblies = new PKScopeArray<ASSEMBLY_t>();
        PK.PARTITION.ask_assemblies(partition, &assemblies.size, &assemblies.data);
        Queue<PK.ASSEMBLY_t> assemblyQueue = new();
        Queue<Matrix4x4> matrixQueue = new();
        for (int i = 0; i < assemblies.size; i++)
        {
            using var refInstances = new PKScopeArray<INSTANCE_t>();
            PK.PART.ask_ref_instances(assemblies[i], &refInstances.size, &refInstances.data);
            if (refInstances.size == 0)
            {
                assemblyQueue.Enqueue(assemblies[i]);
                matrixQueue.Enqueue(Matrix4x4.Identity);
            }
        }
        Console.WriteLine($"assemblies count:{assemblies.size}");
        while (assemblyQueue.Count > 0)
        {
            var assembly = assemblyQueue.Dequeue();
            var asmMatrix = matrixQueue.Dequeue();
            using var instances = new PKScopeArray<PK.INSTANCE_t>();
            PK.ASSEMBLY.ask_instances(assembly, &instances.size, &instances.data);
            for (int j = 0; j < instances.size; j++)
            {
                PK.INSTANCE.ask(instances[j], &instanceSF);
                PK.ENTITY.ask_class(instanceSF.part, &classSF);
                if (instanceSF.transf == PK.ENTITY_t.@null)
                {
                    instanceSF.transf = identityTrans;
                }
                PK.TRANSF.ask(instanceSF.transf, &transformSF);
                var transPtr = (double*)&transformSF;
                Matrix4x4 matrix;
                var matrixPtr = (float*)&matrix;
                for (int i = 0; i < 16; i++)
                {
                    matrixPtr[i] = (float)transPtr[i];
                }
                matrix = asmMatrix * matrix;
                switch (classSF)
                {
                    case PK.CLASS_t.body:
                        bodiesSet.Remove(instanceSF.part);
                        var shadedGeometry = goCallback.GetShadedGeometry(instanceSF.part);
                        asmGeom.AddCompnent(shadedGeometry, Matrix4x4.Transpose(matrix));
                        var wireframeGeometry = goCallback.GetWireframeGeometry(instanceSF.part);
                        asmGeom.AddCompnent(wireframeGeometry, Matrix4x4.Transpose(matrix));
                        break;
                    case PK.CLASS_t.assembly:
                        assemblyQueue.Enqueue(instanceSF.part);
                        matrixQueue.Enqueue(matrix);
                        break;
                    default:
                        continue;
                }

            }
        }
        geometry = asmGeom;
        Console.WriteLine($"bodiesSet count:{bodiesSet.Count}");
        foreach (var body in bodiesSet)
        {
            var shadedGeometry = goCallback.GetShadedGeometry(body);
            asmGeom.AddCompnent(shadedGeometry, Matrix4x4.Identity);
            var wireframeGeometry = goCallback.GetWireframeGeometry(body);
            asmGeom.AddCompnent(wireframeGeometry, Matrix4x4.Identity);
        }
        watch.Stop();
        Console.WriteLine($"get part geometry elapsed time:{watch.ElapsedMilliseconds} ms");
    }
}