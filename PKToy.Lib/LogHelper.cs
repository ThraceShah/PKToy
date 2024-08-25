namespace PKToy.Lib;
class LogHelper
{
    public static void PrintMethodName([CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"Method: {methodName}");
    }

    public static void PrintParameters<T1>(T1 I, [CallerArgumentExpression("I")] string IName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>");
    }

    public static void PrintParameters<T1, T2>(T1 I, T2 II, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>");
    }

    public static void PrintParameters<T1, T2, T3>(T1 I, T2 II, T3 III, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>");
    }

    public static void PrintParameters<T1, T2, T3, T4>(T1 I, T2 II, T3 III, T4 IV, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>");
    }

    public static void PrintParameters<T1, T2, T3, T4, T5>(T1 I, T2 II, T3 III, T4 IV, T5 V, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerArgumentExpression("V")] string VName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>, <{VName},{V}>");
    }

    public static void PrintParameters<T1, T2, T3, T4, T5, T6>(T1 I, T2 II, T3 III, T4 IV, T5 V, T6 VI, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerArgumentExpression("V")] string VName = "", [CallerArgumentExpression("VI")] string VIName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>, <{VName},{V}>, <{VIName},{VI}>");
    }

    public static void PrintParameters<T1, T2, T3, T4, T5, T6, T7>(T1 I, T2 II, T3 III, T4 IV, T5 V, T6 VI, T7 VII, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerArgumentExpression("V")] string VName = "", [CallerArgumentExpression("VI")] string VIName = "", [CallerArgumentExpression("VII")] string VIIName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>, <{VName},{V}>, <{VIName},{VI}>, <{VIIName},{VII}>");
    }

    public static void PrintParameters<T1, T2, T3, T4, T5, T6, T7, T8>(T1 I, T2 II, T3 III, T4 IV, T5 V, T6 VI, T7 VII, T8 VIII, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerArgumentExpression("V")] string VName = "", [CallerArgumentExpression("VI")] string VIName = "", [CallerArgumentExpression("VII")] string VIIName = "", [CallerArgumentExpression("VIII")] string VIIIName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>, <{VName},{V}>, <{VIName},{VI}>, <{VIIName},{VII}>, <{VIIIName},{VIII}>");
    }

    public static void PrintParameters<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 I, T2 II, T3 III, T4 IV, T5 V, T6 VI, T7 VII, T8 VIII, T9 IX, [CallerArgumentExpression("I")] string IName = "",
        [CallerArgumentExpression("II")] string IIName = "", [CallerArgumentExpression("III")] string IIIName = "", [CallerArgumentExpression("IV")] string IVName = "", [CallerArgumentExpression("V")] string VName = "", [CallerArgumentExpression("VI")] string VIName = "", [CallerArgumentExpression("VII")] string VIIName = "", [CallerArgumentExpression("VIII")] string VIIIName = "", [CallerArgumentExpression("IX")] string IXName = "", [CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"MethodName:{methodName}, Parameters: <{IName},{I}>, <{IIName},{II}>, <{IIIName},{III}>, <{IVName},{IV}>, <{VName},{V}>, <{VIName},{VI}>, <{VIIName},{VII}>, <{VIIIName},{VIII}>, <{IXName},{IX}>");
    }
}