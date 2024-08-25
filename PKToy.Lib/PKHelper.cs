using System;
using System.Runtime.CompilerServices;
using System.Text;
namespace PKToy.Lib;
public static class PKHelper
{
    public static unsafe void PrintParameters(int* guise, int* format, byte* name, int* namlen, int* skiphd, int* strid, int* ifail, [CallerMemberName] string methodName = "")
    {
        string? nameStr = new string((sbyte*)name, 0, *namlen, Encoding.UTF8);
        string? guiseName = Enum.GetName((UNCLASSED.file_guise_tokens_t)(*guise));
        string? formatName = Enum.GetName((UNCLASSED.file_format_tokens_t)(*format));
        string? skiphdName = Enum.GetName((UNCLASSED.file_open_mode_tokens_t)(*skiphd));
        string? ifailName = Enum.GetName((UNCLASSED.frustrum_ifails_t)(*ifail));
        Console.WriteLine($"MethodName:{methodName}, Parameters: <guise,{guiseName}>, <format,{formatName}>, <name,{nameStr}>, <namlen,{*namlen}>, <skiphd,{skiphdName}>, <strid,{*strid}>, <ifail,{ifailName}>");
    }

    public static unsafe void PrintParameters(int* guise, int* format, byte* name, int* namlen, byte* pd2hdr, int* pd2len, int* strid, int* ifail, [CallerMemberName] string methodName = "")
    {
        string nameStr = new string((sbyte*)name, 0, *namlen, Encoding.UTF8);
        string pd2hdrStr = new string((sbyte*)pd2hdr, 0, *pd2len, Encoding.UTF8);
        string? guiseName = Enum.GetName((UNCLASSED.file_guise_tokens_t)(*guise));
        string? formatName = Enum.GetName((UNCLASSED.file_format_tokens_t)(*format));
        string? ifailName = Enum.GetName((UNCLASSED.frustrum_ifails_t)(*ifail));
        Console.WriteLine($"MethodName:{methodName}, Parameters: <guise,{guiseName}>, <format,{formatName}>, <name,{nameStr}>, <namlen,{*namlen}>, <pd2hdr,{pd2hdrStr}>, <pd2len,{*pd2len}>, <strid,{*strid}>, <ifail,{ifailName}>");
    }

    public static unsafe void PrintParameters(int* guise, int* strid, int* nchars, byte* buffer, int* ifail, [CallerMemberName] string methodName = "")
    {
        string bufferStr = new string((sbyte*)buffer, 0, *nchars, Encoding.UTF8);
        string? guiseName = Enum.GetName((UNCLASSED.file_guise_tokens_t)(*guise));
        string? ifailName = Enum.GetName((UNCLASSED.frustrum_ifails_t)(*ifail));
        Console.WriteLine($"MethodName:{methodName}, Parameters: <guise,{guiseName}>, <strid,{*strid}>, <nchars,{*nchars}>, <buffer,{bufferStr}>, <ifail,{ifailName}>");
    }

    public static unsafe void PrintParameters(int* guise, int* strid, int* nmax, byte* buffer, int* nactual, int* ifail, [CallerMemberName] string methodName = "")
    {
        string bufferStr = new string((sbyte*)buffer, 0, *nactual, Encoding.UTF8);
        string? guiseName = Enum.GetName((UNCLASSED.file_guise_tokens_t)(*guise));
        string? ifailName = Enum.GetName((UNCLASSED.frustrum_ifails_t)(*ifail));
        Console.WriteLine($"MethodName:{methodName}, Parameters: <guise,{guiseName}>, <strid,{*strid}>, <nmax,{*nmax}>, <buffer,{bufferStr}>, <nactual,{*nactual}>, <ifail,{ifailName}>");
    }

    public static unsafe void PrintParameters(int* guise, int* strid, int* action, int* ifail, [CallerMemberName] string methodName = "")
    {
        string? guiseName = Enum.GetName((UNCLASSED.file_guise_tokens_t)(*guise));
        string? ifailName = Enum.GetName((UNCLASSED.frustrum_ifails_t)(*ifail));
        Console.WriteLine($"MethodName:{methodName}, Parameters: <guise,{guiseName}>, <strid,{*strid}>, <action,{*action}>, <ifail,{ifailName}>");
    }
}