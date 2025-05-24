using Exchange.Midlayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Exchange.Step2Mid;

enum ApVersion
{
    NotSupported = 0,
    AP203,
    AP203E2,
    AP214E3,
    AP242,
}
public class Step2Mid
{
    private static ApVersion GetStepFileApVersion(string stepFile)
    {
        using var fileStream = new FileStream(stepFile, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream, Encoding.ASCII);
        var sb = new StringBuilder();
        // read byte by byte util find ';',then we get a line string
        while (true)
        {
            var b = reader.ReadByte();
            if (b == '\n' || b == '\r')
            {
                continue;
            }
            if (b == ';')
            {
                var line = sb.ToString();
                sb.Clear();
                if (line.StartsWith("FILE_SCHEMA", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }
                if (line.Contains("CONFIG_CONTROL_DESIGN", StringComparison.OrdinalIgnoreCase))
                {
                    return ApVersion.AP203;
                }
                else if (line.Contains("AP203_CONFIGURATION_CONTROLLED_3D_DESIGN_OF_MECHANICAL_PARTS_AND_ASSEMBLIES_MIM_LF", StringComparison.OrdinalIgnoreCase))
                {
                    return ApVersion.AP203E2;
                }
                else if (line.Contains("AUTOMOTIVE_DESIGN", StringComparison.OrdinalIgnoreCase))
                {
                    return ApVersion.AP214E3;
                }
                else if (line.Contains("AP242_MANAGED_MODEL_BASED_3D_ENGINEERING_MIM_LF", StringComparison.OrdinalIgnoreCase))
                {
                    return ApVersion.AP242;
                }
                break;
            }
            sb.Append((char)b);
        }
        return ApVersion.NotSupported;
    }

    public static MidMgr ResolveStep2Mid(string stepFile)
    {
        var apVersion = GetStepFileApVersion(stepFile);
        switch (apVersion)
        {
            case ApVersion.AP203:
                return Ap203.Step2Mid.ResolveStep2Mid(stepFile);
            case ApVersion.AP203E2:
                return Ap203e2.Step2Mid.ResolveStep2Mid(stepFile);
            case ApVersion.AP214E3:
                return Ap214e3.Step2Mid.ResolveStep2Mid(stepFile);
            default:
                throw new NotSupportedException($"Unsupported AP version: {apVersion}");
        }
    }
}
