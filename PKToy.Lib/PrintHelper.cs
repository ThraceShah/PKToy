using System.Text;
using NativeCorLib;
using PKToy.Lib;

unsafe class PrintHelper
{
    public static void PrintTopolTable(int nTopols, PK.TOPOL_t* topols, PK.CLASS_t* classes, int nRelations, int* parents, int* children, PK.TOPOL.sense_t* senses)
    {
        Console.WriteLine($"topols count: {nTopols}");
        for (var i = 0; i < nTopols; i++)
        {
            Console.Write($"[{i}] {classes[i]}: ");
        }
        Console.WriteLine();
        Console.WriteLine($"relations count: {nRelations}");
        PK.CLASS_t lastType = PK.CLASS_t.@null;
        for (var i = 0; i < nRelations; i++)
        {
            var pkParent = parents[i];
            var pkChild = children[i];
            var pClass = classes[pkParent];
            var cClass = classes[pkChild];
            if (lastType != pClass)
            {
                Console.WriteLine();
                lastType = pClass;
            }
            Console.WriteLine($"[{i}] {pkParent}:{pClass} - {pkChild}:{cClass}-> {senses[i]}");
        }
    }

    public static void PrintTopolGenCode(PK.BODY.type_t bodyType, int nTopols, PK.CLASS_t* classes, int nRelations, int* parents, int* children, PK.TOPOL.sense_t* senses)
    {
        var clBuilder = new StringBuilder("Span<PK.CLASS_t> classes = [");
        var pBuilder = new StringBuilder("Span<int> parents = [");
        var cBuilder = new StringBuilder("Span<int> children = [");
        var sBuilder = new StringBuilder("Span<PK.TOPOL.sense_t> senses = [");
        for (var i = 0; i < nTopols; i++)
        {
            clBuilder.Append($"PK.CLASS_t.{classes[i]}, ");
        }
        clBuilder.Append("];");
        for (var i = 0; i < nRelations; i++)
        {
            pBuilder.Append($"{parents[i]}, ");
            cBuilder.Append($"{children[i]}, ");
            sBuilder.Append($"PK.TOPOL.sense_t.{senses[i]}, ");
        }
        pBuilder.Append("];");
        cBuilder.Append("];");
        sBuilder.Append("];");
        Console.WriteLine(clBuilder.ToString());
        Console.WriteLine(pBuilder.ToString());
        Console.WriteLine(cBuilder.ToString());
        Console.WriteLine(sBuilder.ToString());
        Console.WriteLine($"PK.BODY.create_topology_2_o_t op = new(PK.BODY.type_t.{bodyType})");
        Console.WriteLine($"PK.BODY.create_topology_2(classes.Length, ref classes[0], relations.Length, ref parents[0], ref children[0], ref senses[0], &op, );");

    }

}