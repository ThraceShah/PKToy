using NativeCorLib;
using PKToy.Lib;

class PrintHelper
{
    public static void PrintTopoTable(Span<PK.CLASS_t> pkTopoClassList, Span<int> pkParentList, Span<int> pkChildList, Span<PK.TOPOL.sense_t> pkSenceList)
    {
        Console.WriteLine($"topols count: {pkTopoClassList.Length}");
        for (var i = 0; i < pkTopoClassList.Length; i++)
        {
            Console.Write($"[{i}] {pkTopoClassList[i]}: ");
        }
        Console.WriteLine();
        Console.WriteLine($"relations count: {pkParentList.Length}");
        PK.CLASS_t lastType = PK.CLASS_t.@null;
        for (var i = 0; i < pkParentList.Length; i++)
        {
            var pkParent = pkParentList[i];
            var pkChild = pkChildList[i];
            var pClass = pkTopoClassList[pkParent];
            var cClass = pkTopoClassList[pkChild];
            if (lastType != pClass)
            {
                Console.WriteLine();
                lastType = pClass;
            }
            Console.WriteLine($"[{i}] {pkParent}:{pClass} - {pkChild}:{cClass}-> {pkSenceList[i]}");
        }
    }

}