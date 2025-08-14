// StoragePickupPoints.cs
using UnityEngine;
using System.Collections.Generic;

public class StoragePickupPoints : MonoBehaviour
{
    public List<Transform> points = new();        // расставь пустышки-дети вокруг склада
    readonly HashSet<Transform> busy = new();
    public bool TryAcquire(out Transform p)
    {
        foreach (var t in points) if (t && !busy.Contains(t)) { busy.Add(t); p = t; return true; }
        p = null; return false;
    }
    public void Release(Transform p){ if (p) busy.Remove(p); }
}
