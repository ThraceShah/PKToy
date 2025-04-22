
using System.Diagnostics;
using System.Numerics;
using System.Text;
using NativeCorLib;
using Viewer.IContract;

namespace PKToy.Lib;

public unsafe class PKSession
{
    public static void Init()
    {
        Frustrum.InitializeParasolidFrustrum();
        NewSession();
    }

    public static void StopSession()
    {
        PK.SESSION.stop();
    }

    public static void NewSession()
    {
        PKErrorCheck err;
        PK.DELTA.frustrum_t deltaFru = new()
        {
            open_for_write_fn = &FrustrumDelta.OpenForWrite,
            write_fn = &FrustrumDelta.Write,
            open_for_read_fn = &FrustrumDelta.OpenForRead,
            read_fn = &FrustrumDelta.Read,
            delete_fn = &FrustrumDelta.Delete,
            close_fn = &FrustrumDelta.Close,
        };
        err = PK.DELTA._register_callbacks(deltaFru);

        PK.SESSION.start_o_t start_options = new(true);
        err = PK.SESSION.start(&start_options);
        err = PK.SESSION.set_unicode(true);
        err = PK.SESSION.set_roll_forward(true);
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


    public static AsmGeometry OpenPart(string partName, out int partionTag)
    {
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
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        PK.PARTITION_t newPartition;
        PK.PARTITION.create_empty(&newPartition);
        PK.PARTITION.set_current(newPartition);

        using var parts = new PKScopeArray<PK.PART_t>();
        PK.PART.receive(partName, &receive_options, &parts.size, &parts.data);
        var asmGeom = OpenPartition(newPartition);
        PK.PARTITION.set_current(curPartition);
        partionTag = newPartition;
        return asmGeom;
    }

    public static AsmGeometry OpenPartition(PK.PARTITION_t partition)
    {
        var watch = new Stopwatch();
        watch.Start();
        using var bodies = new PKScopeArray<PK.BODY_t>();
        PK.PARTITION.ask_bodies(partition, &bodies.size, &bodies.data);
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
                        asmGeom.AddComponent(shadedGeometry, Matrix4x4.Transpose(matrix));
                        var wireframeGeometry = goCallback.GetWireframeGeometry(instanceSF.part);
                        asmGeom.AddComponent(wireframeGeometry, Matrix4x4.Transpose(matrix));
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
            var shadedGeometry = goCallback.GetShadedGeometry(body);
            asmGeom.AddComponent(shadedGeometry, Matrix4x4.Identity);
            var wireframeGeometry = goCallback.GetWireframeGeometry(body);
            asmGeom.AddComponent(wireframeGeometry, Matrix4x4.Identity);
        }
        watch.Stop();
        Console.WriteLine($"get part geometry elapsed time:{watch.ElapsedMilliseconds} ms");
        return asmGeom;
    }

    public static AsmGeometry OpenCurrentPartition()
    {
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        return OpenPartition(curPartition);
    }

    public static AsmGeometry OpenStep(string stepName, out int partionTag)
    {
        var watch = new Stopwatch();
        watch.Start();
        var partition = PKToy.Exchange.StepLoader.LoadStep(stepName);
        watch.Stop();
        partionTag = partition;
        Console.WriteLine($"load step to pk elapsed time:{watch.ElapsedMilliseconds} ms");
        return OpenPartition(partition);
    }

    public static void SavePart(string partName, PK.PARTITION_t partition)
    {
        var parts = new PKScopeArray<PK.PART_t>();
        PK.SESSION.ask_parts(&parts.size, &parts.data);
        PK.PART.transmit_o_t transmitOptions = new(true);
        transmitOptions.transmit_format = PK.transmit_format_t.text_c;
        PK.PART.transmit(parts.size, parts.data, partName, &transmitOptions);
    }

    public static void SavePart(string partName)
    {
        var parts = new PKScopeArray<PK.PART_t>();
        PK.SESSION.ask_parts(&parts.size, &parts.data);
        PK.PART.transmit_o_t transmitOptions = new(true);
        var extension = Path.GetExtension(partName).ToLower();
        if (extension is ".x_t")
        {
            transmitOptions.transmit_format = PK.transmit_format_t.text_c;
        }
        else if (extension is ".x_b")
        {
            transmitOptions.transmit_format = PK.transmit_format_t.binary_c;
        }
        else
        {
            throw new NotSupportedException("Unsupported file format");
        }
        PK.PART.transmit(parts.size, parts.data, partName, &transmitOptions);
    }

    public static List<TopolTable> GetCurPartitionTopolTree()
    {
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        return GetPartitionTopolTree(curPartition);
    }

    public static List<TopolTable> GetPartitionTopolTree(int partition)
    {
        var topoTree = new List<TopolTable>();
        using var bodies = new PKScopeArray<PK.BODY_t>();
        PK.PARTITION.ask_bodies(partition, &bodies.size, &bodies.data);
        var printInfo = bodies.size == 1;
        foreach (var body in bodies.Span)
        {
            topoTree.Add(GetBodyTopolTree(body, printInfo));
        }
        return topoTree;
    }

    public static TopolNode[] GetCurPartitionBodyNodes()
    {
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        return GetPartitionBodyNodes(curPartition);
    }


    public static TopolNode[] GetPartitionBodyNodes(int partition)
    {
        using var bodies = new PKScopeArray<PK.BODY_t>();
        PK.PARTITION.ask_bodies(partition, &bodies.size, &bodies.data);
        var result = new TopolNode[bodies.size];
        for (int i = 0; i < bodies.size; i++)
        {
            var body = bodies[i];
            result[i] = new TopolNode(body, GetEntityName(body, out _));
        }
        return result;
    }


    public static TopolTable? GetEntityTable(int entity)
    {
        PK.CLASS_t cl;
        PK.ENTITY.ask_class(entity, &cl);
        if (cl == PK.CLASS_t.body)
        {
            return GetBodyTopolTree((PK.BODY_t)entity, true);
        }
        return null;
    }



    private static string BodyTypeTostring(BODY.type_t bodyType) => bodyType switch
    {
        BODY.type_t.solid_c => "solid",
        BODY.type_t.sheet_c => "sheet",
        BODY.type_t.minimum_c => "minimum",
        BODY.type_t.wire_c => "wire",
        BODY.type_t.general_c => "general",
        BODY.type_t.acorn_c => "acorn",
        BODY.type_t.unspecified_c => "unspecified",
        BODY.type_t.empty_c => "empty",
        BODY.type_t.compound_c => "compound",
        _ => throw new NotImplementedException(),
    };


    private static string GetEntityName(PK.ENTITY_t entity, out CLASS_t entityType)
    {
        PK.CLASS_t cl;
        PK.ENTITY.ask_class(entity, &cl);
        entityType = cl;
        if (cl == PK.CLASS_t.body)
        {
            PK.BODY.type_t bodyType;
            PK.BODY.ask_type(entity, &bodyType);
            return BodyTypeTostring(bodyType);
        }
        else if (cl == PK.CLASS_t.region)
        {
            PK.LOGICAL_t isSolid;
            PK.REGION.is_solid(entity, &isSolid);
            string regionType = isSolid ? "solid" : "void";
            return $"{cl}({regionType})";
        }
        else if (cl == PK.CLASS_t.shell)
        {
            PK.SHELL.type_t shellType;
            PK.SHELL.ask_type(entity, &shellType);
            return $"{cl}({shellType})";
        }
        return cl.ToString();
    }

    private static TopolTable GetBodyTopolTree(PK.BODY_t body, bool printInfo = false)
    {
        PK.BODY.type_t bodyType;
        PK.BODY.ask_type(body, &bodyType);
        using var faces = new PKScopeArray<PK.FACE_t>();
        PK.BODY.ask_topology_o_t options = new(true)
        {
            want_fins = true
        };
        using var topols = new PKScopeArray<PK.TOPOL_t>();
        using var classes = new PKScopeArray<PK.CLASS_t>();
        using var parents = new PKScopeArray<int>();
        using var children = new PKScopeArray<int>();
        using var senses = new PKScopeArray<PK.TOPOL.sense_t>();
        PK.BODY.ask_topology(body, &options, &topols.size, &topols.data, &classes.data, &parents.size, &parents.data, &children.data, &senses.data);
        if (printInfo)
        {
            PrintHelper.PrintTopolTable(topols.size, topols.data, classes.data, parents.size, parents.data, children.data, senses.data);
            PrintHelper.PrintTopolGenCode(bodyType, topols.size, classes.data, parents.size, parents.data, children.data, senses.data);
        }
        var relations = new List<TopolRelation>(topols.size * 2);
        var nodes = new List<TopolNode>(topols.size * 2);
        using var faceList = new UMList<int>(topols.size);
        using var finList = new UMList<int>(topols.size);
        using var edgeList = new UMList<int>(topols.size);
        using var vertexList = new UMList<int>(topols.size);
        for (int i = 0; i < topols.size; i++)
        {
            var topol = topols[i];
            var topolNode = new TopolNode(topol, GetEntityName(topol, out var entityType));
            nodes.Add(topolNode);
            switch (entityType)
            {
                case PK.CLASS_t.face:
                    faceList.Add(i);
                    break;
                case PK.CLASS_t.fin:
                    finList.Add(i);
                    break;
                case PK.CLASS_t.edge:
                    edgeList.Add(i);
                    break;
                case PK.CLASS_t.vertex:
                    vertexList.Add(i);
                    break;
                default:
                    break;
            }
        }
        for (int i = 0; i < parents.size; i++)
        {
            var parent = parents[i];
            var child = children[i];
            var parentNode = nodes[parent];
            var childNode = nodes[child];
            var sense = $"{parentNode.Tag}-->{childNode.Tag}:{senses[i]}";
            relations.Add(new(parent, child, sense));
        }
        foreach (var faceIndex in faceList)
        {
            var topolNode = nodes[faceIndex];
            var face = topolNode.Tag;
            PK.SURF_t geom;
            PK.LOGICAL_t orient;
            PK.FACE.ask_oriented_surf(face, &geom, &orient);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}({(bool)orient})");
            var sense = $"{face}-->{geom.Value}:{orient}";
            relations.Add(new(faceIndex, nodes.Count, sense));
            nodes.Add(geomNode);

        }
        foreach (var finIndex in finList)
        {
            var topolNode = nodes[finIndex];
            var fin = topolNode.Tag;
            PK.CURVE_t geom;
            PK.FIN.ask_curve(fin, &geom);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}");
            var sense = $"{fin}-->{geom.Value}";
            relations.Add(new(finIndex, nodes.Count, sense));
            nodes.Add(geomNode);

        }
        foreach (var edgeIndex in edgeList)
        {
            var topolNode = nodes[edgeIndex];
            var edge = topolNode.Tag;
            PK.CURVE_t geom;
            PK.LOGICAL_t orient;
            PK.EDGE.ask_oriented_curve(edge, &geom, &orient);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var sense = $"{edge}-->{geom.Value}:{orient}";
            var geomNode = new TopolNode(geom, $"{geomName}({(bool)orient})");
            relations.Add(new(edgeIndex, nodes.Count, sense));
            nodes.Add(geomNode);
        }
        foreach (var vertexIndex in vertexList)
        {
            var topolNode = nodes[vertexIndex];
            var vertex = topolNode.Tag;
            PK.POINT_t geom;
            PK.VERTEX.ask_point(vertex, &geom);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}");
            var sense = $"{vertex}-->{geom.Value}";
            relations.Add(new(vertexIndex, nodes.Count, sense));
            nodes.Add(geomNode);

        }
        return new(nodes, relations);
    }
}

public record TopolNode(int Tag, string TypeName = "")
{
    private readonly string _header = $"{TypeName}:{Tag}";
    public string Header => _header;
}

public record TopolRelation(int Parent, int Child, string Sense);
public record TopolTable(List<TopolNode> Nodes, List<TopolRelation> Relations);