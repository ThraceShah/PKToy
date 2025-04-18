using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
namespace PKToy.Script;

public class CsxRunner
{
    public static async ValueTask<bool> Run(string scriptPath)
    {
        try
        {
            var options = ScriptOptions.Default
            .AddReferences("pskernel_net")
            .AddImports("System")
            .AddImports("System.Collections.Generic")
            .AddImports("System.IO")
            .AddImports("PK");
            var scriptState = await CSharpScript.RunAsync(File.ReadAllText(scriptPath), options);
            Console.WriteLine(scriptState.ReturnValue);
            return true;
        }
        catch (CompilationErrorException e)
        {
            Console.WriteLine($"Script compilation failed: {string.Join(Environment.NewLine, e.Diagnostics)}");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Script execution failed: {e.Message}");
            return false;
        }
    }
}
