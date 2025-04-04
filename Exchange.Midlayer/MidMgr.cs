using System.Numerics;

namespace Exchange.Midlayer;

public class MidMgr
{
    private readonly LinkedList<IMidObj> midObjList = new();
    private readonly Dictionary<ExpId, IMidObj> expIdMidObjMap = [];
    private readonly Dictionary<ImpId, IMidObj> impIdMidObjMap = [];

    public T GetOrCreateMidObj<T>(ImpId impId) where T : IMidObj, new()
    {
        if (impIdMidObjMap.TryGetValue(impId, out var midObj))
        {
            return (T)midObj;
        }
        midObj = new T { ImpId = impId };
        impIdMidObjMap[impId] = midObj;
        midObjList.AddLast(midObj);
        return (T)midObj;
    }

    public T GetMidObjByImp<T>(ImpId impId) where T : IMidObj
    {
        if (impIdMidObjMap.TryGetValue(impId, out var midObj))
        {
            return (T)midObj;
        }
        throw new Exception($"MidObj not found for ImpId #{impId}");
    }

    public T GetMidObjByExp<T>(ExpId expId) where T : IMidObj
    {
        if (expIdMidObjMap.TryGetValue(expId, out var midObj))
        {
            return (T)midObj;
        }
        throw new Exception($"MidObj not found for ExpId #{expId}");
    }

    public T CreateMidObj<T>() where T : IMidObj, new()
    {
        var midObj = new T();
        midObjList.AddLast(midObj);
        return midObj;
    }

    public void SetObjExpId(IMidObj midObj, ExpId expId)
    {
        if (midObj.ExpId != 0)
        {
            expIdMidObjMap.Remove(midObj.ExpId);
        }
        midObj.ExpId = expId;
        expIdMidObjMap[expId] = midObj;
    }

    public IEnumerable<T> GetMidObjs<T>() where T : IMidObj
    {
        return midObjList.OfType<T>();
    }
}