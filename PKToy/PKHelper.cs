namespace PKToy;
class PKHelper
{
    private static void PrintMethodName([CallerMemberName] string methodName = "")
    {
        Console.WriteLine($"Method: {methodName}");
    }

}