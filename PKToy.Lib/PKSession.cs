
using System.Diagnostics;
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
        var watch = new Stopwatch();
        watch.Start();
        using var parts = new PKScopeArray<PK.PART_t>();
        err = PK.PART.receive(partName, &receive_options, &parts.size, &parts.data);

        var partitions = new PKScopeArray<PK.PARTITION_t>();
        err = PK.SESSION.ask_partitions(&partitions.size, &partitions.data);
        watch.Stop();
        Console.WriteLine($"kernel load model elapsed time:{watch.ElapsedMilliseconds} ms");
        // PK.PARTITION_t partition;
        // err =PK.SESSION.ask_curr_partition(&partition);
        using var bodies = new PKScopeArray<PK.BODY_t>();
        err =PK.PARTITION.ask_bodies(partitions[0], &bodies.size, &bodies.data);
        watch.Restart();
        using var goCallback=new PKGoCallback();
        Console.WriteLine("render faces");
        PK.TOPOL.render_facet_o_t facet_options = new(true);
        facet_options.go_option.go_normals = facet_go_normals_t.yes_c;
        facet_options.go_option.go_edges = facet_go_edges_t.yes_c;
        facet_options.go_option.go_strips = facet_go_strips_t.yes_c;
        facet_options.go_option.go_max_facets_per_strip=65535;
        err = PK.TOPOL.render_facet(bodies.size, (TOPOL_t*)bodies.data, null, 0, &facet_options);
        watch.Stop();
        Console.WriteLine($"render facet elapsed time:{watch.ElapsedMilliseconds} ms");
        watch.Restart();
        var bodiesSet=new HashSet<PK.BODY_t>();
        for (int i = 0; i < bodies.size; i++)
        {
            bodiesSet.Add(bodies[i]);
        }
        var bodyIndexMap=new Dictionary<BODY_t,int>();
        var partGeometries=new PartGeometry[bodies.size];
        for (int i = 0; i < bodies.size; i++)
        {
            var body = bodies[i];
            partGeometries[i] = goCallback.GetPartGeometry(body);
            bodyIndexMap.Add(body, i);
        }

        var compGeometries=new List<CompGeometry>();
        var identityTransform = new TRANSF_sf_t(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        TRANSF_t identityTrans;
        PK.TRANSF.create(&identityTransform, &identityTrans);
        INSTANCE_sf_t instanceSF;
        TRANSF_sf_t transformSF;
        CLASS_t classSF;
        using var assemblies=new PKScopeArray<ASSEMBLY_t>();
        err =PK.PARTITION.ask_assemblies(partitions[0], &assemblies.size, &assemblies.data);
        Queue<PK.ASSEMBLY_t> assemblyQueue=new();
        Queue<Matrix4x4> matrixQueue=new();
        for (int i = 0; i < assemblies.size; i++)
        {
            using var refInstances=new PKScopeArray<INSTANCE_t>();
            PK.PART.ask_ref_instances(assemblies[i], &refInstances.size, &refInstances.data);
            if(refInstances.size==0)
            {
                assemblyQueue.Enqueue(assemblies[i]);
                matrixQueue.Enqueue(Matrix4x4.Identity);
            }
        }
        Console.WriteLine($"assemblies count:{assemblies.size}");
        while(assemblyQueue.Count>0)
        {
            var assembly=assemblyQueue.Dequeue();
            var asmMatrix=matrixQueue.Dequeue();
            using var instances=new PKScopeArray<PK.INSTANCE_t>();
            err =PK.ASSEMBLY.ask_instances(assembly, &instances.size, &instances.data);
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
                matrix=asmMatrix*matrix;
                switch (classSF)
                {
                    case PK.CLASS_t.body:
                        bodiesSet.Remove(instanceSF.part);
                        var partGeometry = goCallback.GetPartGeometry(instanceSF.part);
                        compGeometries.Add(new CompGeometry
                        {
                            PartIndex= (uint)bodyIndexMap[instanceSF.part],
                            CompMatrix=Matrix4x4.Transpose(matrix),
                        });
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
        Console.WriteLine($"bodiesSet count:{bodiesSet.Count}");
        foreach (var body in bodiesSet)
        {
            var index=bodyIndexMap[body];
            compGeometries.Add(new CompGeometry
            {
                PartIndex= (uint)index,
                CompMatrix=Matrix4x4.Identity,
            });
        }
        geometry = new AsmGeometry(partGeometries,[..compGeometries]);
        watch.Stop();
        Console.WriteLine($"get part geometry elapsed time:{watch.ElapsedMilliseconds} ms");
    }
}