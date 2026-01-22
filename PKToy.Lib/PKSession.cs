
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
        PK_SESSION_stop();
    }

    public static void NewSession()
    {
        PKErrorCheck err;
        PK_DELTA_frustrum_t deltaFru = new()
        {
            open_for_write_fn = &FrustrumDelta.OpenForWrite,
            write_fn = &FrustrumDelta.Write,
            open_for_read_fn = &FrustrumDelta.OpenForRead,
            read_fn = &FrustrumDelta.Read,
            delete_fn = &FrustrumDelta.Delete,
            close_fn = &FrustrumDelta.Close,
        };
        err = PK_DELTA_register_callbacks(deltaFru);

        PK_SESSION_start_o_t start_options = new();
        err = PK_SESSION_start(&start_options);
        err = PK_SESSION_set_unicode(PK_LOGICAL_true);
        err = PK_SESSION_set_roll_forward(PK_LOGICAL_true);
        PK_SESSION_smp_o_t smpOptions = new();
        PK_SESSION_set_smp(&smpOptions);
        PK_SESSION_smp_r_t smpResult;
        PK_SESSION_ask_smp(&smpResult);
    }

    public static void CallRenderFacet(Span<PK_BODY_t> bodies)
    {
        var watch = new Stopwatch();
        watch.Start();
        PK_TOPOL_render_facet_o_t facetOptions = new();
        facetOptions.go_option.go_normals = PK_facet_go_normals_yes_c;
        facetOptions.go_option.go_edges = PK_facet_go_edges_yes_c;
        facetOptions.go_option.go_strips = PK_facet_go_strips_yes_c;
        facetOptions.go_option.go_max_facets_per_strip = 65535;
        facetOptions.go_option.go_interleaved = PK_facet_go_interleaved_yes_c;
        PK_TOPOL_render_facet(bodies.Length, (PK_TOPOL_t*)Unsafe.AsPointer(ref bodies[0]), null, 0, &facetOptions);
        watch.Stop();
        Console.WriteLine($"render facet elapsed time:{watch.ElapsedMilliseconds} ms");
        return;
    }


    public static AsmGeometry OpenPart(string partName, out int partionTag)
    {
        PK_PART_receive_o_t receive_options = new();
        if (Path.GetExtension(partName).Equals(".x_t", StringComparison.OrdinalIgnoreCase))
        {
            receive_options.transmit_format = PK_transmit_format_text_c;
        }
        else if (Path.GetExtension(partName).Equals(".x_b", StringComparison.OrdinalIgnoreCase))
        {
            receive_options.transmit_format = PK_transmit_format_binary_c;
        }
        else
        {
            throw new NotSupportedException("Unsupported file format");
        }
        var watch = new Stopwatch();
        watch.Start();
        PK_PARTITION_t curPartition;
        PK_SESSION_ask_curr_partition(&curPartition);
        PK_PARTITION_t newPartition;
        PK_PARTITION_create_empty(&newPartition);
        PK_PARTITION_set_current(newPartition);

        using var parts = new PKScopeArray<PK_PART_t>();
        PK_PART_receive(partName, &receive_options, &parts.size, &parts.data);
        var asmGeom = OpenPartition(newPartition);
        PK_PARTITION_set_current(curPartition);
        partionTag = newPartition;
        return asmGeom;
    }

    public static AsmGeometry OpenPartition(PK_PARTITION_t partition)
    {
        var watch = new Stopwatch();
        watch.Start();
        using var bodies = new PKScopeArray<PK_BODY_t>();
        PK_PARTITION_ask_bodies(partition, &bodies.size, &bodies.data);
        using var goCallback = new PKGoCallback();
        Console.WriteLine("render faces");
        CallRenderFacet(bodies.Span);
        var bodiesSet = new HashSet<PK_BODY_t>();
        for (int i = 0; i < bodies.size; i++)
        {
            bodiesSet.Add(bodies[i]);
        }
        var asmGeom = new AsmGeometry();

        var identityTransform = new PK_TRANSF_sf_t(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        PK_TRANSF_t identityTrans;
        PK_TRANSF_create(&identityTransform, &identityTrans);
        PK_INSTANCE_sf_t instanceSF;
        PK_TRANSF_sf_t transformSF;
        PK_CLASS_t classSF;
        using var assemblies = new PKScopeArray<PK_ASSEMBLY_t>();
        PK_PARTITION_ask_assemblies(partition, &assemblies.size, &assemblies.data);
        Queue<PK_ASSEMBLY_t> assemblyQueue = new();
        Queue<Matrix4x4> matrixQueue = new();
        for (int i = 0; i < assemblies.size; i++)
        {
            using var refInstances = new PKScopeArray<PK_INSTANCE_t>();
            PK_PART_ask_ref_instances(assemblies[i], &refInstances.size, &refInstances.data);
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
            using var instances = new PKScopeArray<PK_INSTANCE_t>();
            PK_ASSEMBLY_ask_instances(assembly, &instances.size, &instances.data);
            for (int j = 0; j < instances.size; j++)
            {
                PK_INSTANCE_ask(instances[j], &instanceSF);
                PK_ENTITY_ask_class(instanceSF.part, &classSF);
                if (instanceSF.transf == NULTAG)
                {
                    instanceSF.transf = identityTrans;
                }
                PK_TRANSF_ask(instanceSF.transf, &transformSF);
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
                    case PK_CLASS_body:
                        bodiesSet.Remove(instanceSF.part);
                        var shadedGeometry = goCallback.GetShadedGeometry(instanceSF.part);
                        asmGeom.AddComponent(shadedGeometry, Matrix4x4.Transpose(matrix));
                        var wireframeGeometry = goCallback.GetWireframeGeometry(instanceSF.part);
                        asmGeom.AddComponent(wireframeGeometry, Matrix4x4.Transpose(matrix));
                        break;
                    case PK_CLASS_assembly:
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
        PK_PARTITION_t curPartition;
        PK_SESSION_ask_curr_partition(&curPartition);
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

    public static void SavePart(string partName, PK_PARTITION_t partition)
    {
        var parts = new PKScopeArray<PK_PART_t>();
        PK_SESSION_ask_parts(&parts.size, &parts.data);
        PK_PART_transmit_o_t transmitOptions = new();
        transmitOptions.transmit_format = PK_transmit_format_text_c;
        PK_PART_transmit(parts.size, parts.data, partName, &transmitOptions);
    }

    public static void SavePart(string partName)
    {
        var parts = new PKScopeArray<PK_PART_t>();
        PK_SESSION_ask_parts(&parts.size, &parts.data);
        PK_PART_transmit_o_t transmitOptions = new();
        var extension = Path.GetExtension(partName).ToLower();
        if (extension is ".x_t")
        {
            transmitOptions.transmit_format = PK_transmit_format_text_c;
        }
        else if (extension is ".x_b")
        {
            transmitOptions.transmit_format = PK_transmit_format_binary_c;
        }
        else
        {
            throw new NotSupportedException("Unsupported file format");
        }
        PK_PART_transmit(parts.size, parts.data, partName, &transmitOptions);
    }

    public static List<TopolTable> GetCurPartitionTopolTree()
    {
        PK_PARTITION_t curPartition;
        PK_SESSION_ask_curr_partition(&curPartition);
        return GetPartitionTopolTree(curPartition);
    }

    public static List<TopolTable> GetPartitionTopolTree(int partition)
    {
        var topoTree = new List<TopolTable>();
        using var bodies = new PKScopeArray<PK_BODY_t>();
        PK_PARTITION_ask_bodies(partition, &bodies.size, &bodies.data);
        var printInfo = bodies.size == 1;
        foreach (var body in bodies.Span)
        {
            topoTree.Add(GetBodyTopolTree(body));
        }
        return topoTree;
    }

    private static TopolTable GetBodyTable(PK_BODY_t body)
    {
        var topolNode = new TopolNode(body, GetEntityName(body, out _));
        var nodes = new List<TopolNode>() { topolNode };
        var relations = new List<TopolRelation>();
        return new(nodes, relations);
    }


    private static string BodyTypeTostring(PK_BODY_type_t bodyType) => bodyType switch
    {
        PK_BODY_type_solid_c => "solid",
        PK_BODY_type_sheet_c => "sheet",
        PK_BODY_type_minimum_c => "minimum",
        PK_BODY_type_wire_c => "wire",
        PK_BODY_type_general_c => "general",
        PK_BODY_type_acorn_c => "acorn",
        PK_BODY_type_unspecified_c => "unspecified",
        PK_BODY_type_empty_c => "empty",
        PK_BODY_type_compound_c => "compound",
        _ => throw new NotImplementedException(),
    };


    private static string GetEntityName(PK_ENTITY_t entity, out PK_CLASS_t entityType)
    {
        PK_CLASS_t cl;
        PK_ENTITY_ask_class(entity, &cl);
        entityType = cl;
        if (cl == PK_CLASS_body)
        {
            PK_BODY_type_t bodyType;
            PK_BODY_ask_type(entity, &bodyType);
            return BodyTypeTostring(bodyType);
        }
        else if (cl == PK_CLASS_region)
        {
            PK_LOGICAL_t isSolid;
            PK_REGION_is_solid(entity, &isSolid);
            string regionType = isSolid==0 ? "solid" : "void";
            return $"{cl}({regionType})";
        }
        else if (cl == PK_CLASS_shell)
        {
            PK_SHELL_type_t shellType;
            PK_SHELL_ask_type(entity, &shellType);
            return $"{cl}({shellType})";
        }
        return cl.ToString();
    }

    private static TopolTable GetBodyTopolTree(PK_BODY_t body, bool printInfo = false)
    {
        PK_BODY_type_t bodyType;
        PK_BODY_ask_type(body, &bodyType);
        using var faces = new PKScopeArray<PK_FACE_t>();
        PK_BODY_ask_topology_o_t options = new()
        {
            want_fins = PK_LOGICAL_true,
        };
        using var topols = new PKScopeArray<PK_TOPOL_t>();
        using var classes = new PKScopeArray<PK_CLASS_t>();
        using var parents = new PKScopeArray<int>();
        using var children = new PKScopeArray<int>();
        using var senses = new PKScopeArray<PK_TOPOL_sense_t>();
        PK_BODY_ask_topology(body, &options, &topols.size, &topols.data, &classes.data, &parents.size, &parents.data, &children.data, &senses.data);
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
                case PK_CLASS_face:
                    faceList.Add(i);
                    break;
                case PK_CLASS_fin:
                    finList.Add(i);
                    break;
                case PK_CLASS_edge:
                    edgeList.Add(i);
                    break;
                case PK_CLASS_vertex:
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
            PK_SURF_t geom;
            PK_LOGICAL_t orient;
            PK_FACE_ask_oriented_surf(face, &geom, &orient);
            if (geom == NULTAG)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}({orient})");
            var sense = $"{face}-->{geom}:{orient}";
            relations.Add(new(faceIndex, nodes.Count, sense));
            nodes.Add(geomNode);

        }
        foreach (var finIndex in finList)
        {
            var topolNode = nodes[finIndex];
            var fin = topolNode.Tag;
            PK_CURVE_t geom;
            PK_FIN_ask_curve(fin, &geom);
            if (geom == NULTAG)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}");
            var sense = $"{fin}-->{geom}";
            relations.Add(new(finIndex, nodes.Count, sense));
            nodes.Add(geomNode);

        }
        foreach (var edgeIndex in edgeList)
        {
            var topolNode = nodes[edgeIndex];
            var edge = topolNode.Tag;
            PK_CURVE_t geom;
            PK_LOGICAL_t orient;
            PK_EDGE_ask_oriented_curve(edge, &geom, &orient);
            if (geom == NULTAG)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var sense = $"{edge}-->{geom}:{orient}";
            var geomNode = new TopolNode(geom, $"{geomName}({orient})");
            relations.Add(new(edgeIndex, nodes.Count, sense));
            nodes.Add(geomNode);
        }
        foreach (var vertexIndex in vertexList)
        {
            var topolNode = nodes[vertexIndex];
            var vertex = topolNode.Tag;
            PK_POINT_t geom;
            PK_VERTEX_ask_point(vertex, &geom);
            if (geom == NULTAG)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolNode(geom, $"{geomName}");
            var sense = $"{vertex}-->{geom}";
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