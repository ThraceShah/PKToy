using System.Numerics;

namespace Exchange.Midlayer;

public class MidMgr
{
    private readonly Dictionary<ExpId, IMidObj> expIdMidObjMap = [];
    private readonly Dictionary<ImpId, IMidObj> impIdMidObjMap = [];

    private int nextExpId = 1;

    private ExpId GetNextExpId() => nextExpId++;

    public T GetOrCreateMidObj<T>(ImpId impId) where T : IMidObj, new()
    {
        if (impIdMidObjMap.TryGetValue(impId, out var midObj))
        {
            return (T)midObj;
        }
        var expId = GetNextExpId();
        midObj = new T { ImpId = impId, ExpId = expId };
        impIdMidObjMap[impId] = midObj;
        expIdMidObjMap[expId] = midObj;
        return (T)midObj;
    }

    public T GetMidObj<T>(ImpId impId) where T : IMidObj
    {
        if (impIdMidObjMap.TryGetValue(impId, out var midObj))
        {
            return (T)midObj;
        }
        throw new Exception($"MidObj not found for ImpId #{impId}");
    }

    public T CreateMidObj<T>() where T : IMidObj, new()
    {
        var expId = GetNextExpId();
        var midObj = new T { ExpId = expId };
        expIdMidObjMap[expId] = midObj;
        return midObj;
    }

    public IEnumerable<T> GetMidObjs<T>() where T : IMidObj
    {
        return expIdMidObjMap.Values.OfType<T>();
    }
}