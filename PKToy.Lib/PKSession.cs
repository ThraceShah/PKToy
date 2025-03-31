
using System.Diagnostics;
using System.Numerics;
using System.Text;
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


    public static AsmGeometry OpenPart(string partName)
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
        // PK.PARTITION_t newPartition;
        // PK.PARTITION.create_empty(&newPartition);
        // PK.PARTITION.set_current(newPartition);

        using var parts = new PKScopeArray<PK.PART_t>();
        PK.PART.receive(partName, &receive_options, &parts.size, &parts.data);
        // var asmGeom = OpenPartition(newPartition);
        // PK.PARTITION.set_current(curPartition);
        var asmGeom = OpenPartition(curPartition);
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

    public static AsmGeometry OpenStep(string stepName)
    {
        var watch = new Stopwatch();
        watch.Start();
        var partition = PKToy.Exchange.StepLoader.LoadStep(stepName);
        watch.Stop();
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

    public static List<TopolTreeNode> GetCurPartitionTopolTree()
    {
        PK.PARTITION_t curPartition;
        PK.SESSION.ask_curr_partition(&curPartition);
        return GetPartitionTopolTree(curPartition);
    }

    public static List<TopolTreeNode> GetPartitionTopolTree(PK.PARTITION_t partition)
    {
        var topoTree = new List<TopolTreeNode>();
        using var bodies = new PKScopeArray<PK.BODY_t>();
        PK.PARTITION.ask_bodies(partition, &bodies.size, &bodies.data);
        foreach (var body in bodies.Span)
        {
            topoTree.Add(GetBodyTopolTree(body));
        }
        return topoTree;
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

    private static TopolTreeNode GetBodyTopolTree(PK.BODY_t body)
    {
        var topoTree = new TopolTreeNode();
        topoTree.TypeName = "BODY";
        topoTree.Tag = body.Value;
        using var faces = new PKScopeArray<PK.FACE_t>();
        PK.BODY.ask_topology_o_t options = new(true);
        options.want_fins = true;
        using var topols = new PKScopeArray<PK.TOPOL_t>();
        using var classes = new PKScopeArray<PK.CLASS_t>();
        using var parents = new PKScopeArray<int>();
        using var children = new PKScopeArray<int>();
        using var senses = new PKScopeArray<PK.TOPOL.sense_t>();
        PK.BODY.ask_topology(body, &options, &topols.size, &topols.data, &classes.data, &parents.size, &parents.data, &children.data, &senses.data);
        var topolMap = new Dictionary<int, TopolTreeNode>();
        var faceList = new List<PK.FACE_t>();
        var finList = new List<PK.FIN_t>();
        var edgeList = new List<PK.EDGE_t>();
        var vertexList = new List<PK.VERTEX_t>();
        foreach (var topol in topols.Span)
        {
            var topolNode = new TopolTreeNode
            {
                TypeName = GetEntityName(topol, out var entityType),
                Tag = topol
            };
            topolMap[topol] = topolNode;
            switch (entityType)
            {
                case PK.CLASS_t.face:
                    faceList.Add((PK.FACE_t)topol);
                    break;
                case PK.CLASS_t.fin:
                    finList.Add((PK.FIN_t)topol);
                    break;
                case PK.CLASS_t.edge:
                    edgeList.Add((PK.EDGE_t)topol);
                    break;
                case PK.CLASS_t.vertex:
                    vertexList.Add((PK.VERTEX_t)topol);
                    break;
                default:
                    break;
            }
        }
        for (int i = 0; i < parents.size; i++)
        {
            var parent = parents[i];
            var child = children[i];
            var topolParent = topols[parent];
            var topolChild = topols[child];
            var sense = senses[i];
            var parentNode = topolMap[topolParent];
            var childNode = topolMap[topolChild];
            if (childNode.Parents.Contains(parentNode))
            {
                continue;
            }
            parentNode.Children.Add(childNode);
            childNode.Parents.Add(parentNode);
            parentNode.ChilrenSense.Add($"{parent}-->{child}:{sense}");
        }
        foreach (var face in faceList)
        {
            var topolNode = topolMap[face];
            PK.SURF_t geom;
            PK.LOGICAL_t orient;
            PK.FACE.ask_oriented_surf(face, &geom, &orient);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolTreeNode
            {
                TypeName = $"{geomName}({(bool)orient})",
                Tag = geom,
            };
            geomNode.Parents.Add(topolNode);
            topolNode.Children.Add(geomNode);
            topolNode.ChilrenSense.Add($"{face}-->{geom}:{orient}");
        }
        foreach (var fin in finList)
        {
            var topolNode = topolMap[fin];
            PK.CURVE_t geom;
            PK.FIN.ask_curve(fin, &geom);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolTreeNode
            {
                TypeName = $"{geomName}",
                Tag = geom,
            };
            geomNode.Parents.Add(topolNode);
            topolNode.Children.Add(geomNode);
        }
        foreach (var edge in edgeList)
        {
            var topolNode = topolMap[edge];
            PK.CURVE_t geom;
            PK.LOGICAL_t orient;
            PK.EDGE.ask_oriented_curve(edge, &geom, &orient);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolTreeNode
            {
                TypeName = $"{geomName}({(bool)orient})",
                Tag = geom,
            };
            geomNode.Parents.Add(topolNode);
            topolNode.Children.Add(geomNode);
            topolNode.ChilrenSense.Add($"{edge}-->{geom}:{orient}");
        }
        foreach (var vertex in vertexList)
        {
            var topolNode = topolMap[vertex];
            PK.POINT_t geom;
            PK.VERTEX.ask_point(vertex, &geom);
            if (geom == PK.CURVE_t.@null)
            {
                continue;
            }
            var geomName = GetEntityName(geom, out _);
            var geomNode = new TopolTreeNode
            {
                TypeName = $"{geomName}",
                Tag = geom,
            };
            geomNode.Parents.Add(topolNode);
            topolNode.Children.Add(geomNode);
            topolNode.ChilrenSense.Add($"{vertex}-->{geom}");
        }
        return topolMap[body];
    }
}

public class TopolTreeNode
{
    public HashSet<TopolTreeNode> Parents { get; } = [];
    public List<TopolTreeNode> Children { get; } = [];
    public List<string> ChilrenSense { get; } = [];

    public int Tag { get; set; }
    public string TypeName { get; set; } = "";
    public string Headr => $"{TypeName}:{Tag}";
}