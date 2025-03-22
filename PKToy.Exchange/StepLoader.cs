using Exchange.Step2Mid;

namespace PKToy.Exchange;

public class StepLoader
{
    public static PK.PARTITION_t LoadStep(string path)
    {
        var mid = Step2Mid.ResolveStep2Mid(path);
        return Mid2PK.ResolveMid2PK(mid);
    }
}
