using UnityEngine;
using System.Collections.Generic;

public class SlotPoints : MonoBehaviour
{
    [Tooltip("Точки-пустышки, куда встают воркеры для загрузки/разгрузки.")]
    public List<Transform> points = new();

    private readonly HashSet<Transform> busy = new();

    public bool TryAcquire(out Transform slot)
    {
        foreach (var t in points)
        {
            if (t == null) continue;
            if (!busy.Contains(t)) { busy.Add(t); slot = t; return true; }
        }
        slot = null;
        return false;
    }

    public void Release(Transform slot)
    {
        if (slot != null) busy.Remove(slot);
    }

    public int FreeCount()
    {
        int free = 0;
        foreach (var t in points) if (t && !busy.Contains(t)) free++;
        return free;
    }
}