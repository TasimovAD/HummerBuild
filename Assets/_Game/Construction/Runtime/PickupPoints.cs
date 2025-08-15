using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PickupPoints : MonoBehaviour
{
    [Tooltip("Пустышки-точки, куда встают воркеры для операции (забор/выгрузка).")]
    public List<Transform> points = new();

    readonly HashSet<Transform> busy = new();

    /// Попробовать занять свободную точку.
    public bool TryAcquire(out Transform slot)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var t = points[i];
            if (!t) continue;
            if (!busy.Contains(t))
            {
                busy.Add(t);
                slot = t;
                return true;
            }
        }
        slot = null;
        return false;
    }

    /// Освободить точку.
    public void Release(Transform slot)
    {
        if (slot) busy.Remove(slot);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.7f);
        foreach (var p in points)
        {
            if (!p) continue;
            Gizmos.DrawSphere(p.position + Vector3.up * 0.05f, 0.12f);
            Gizmos.DrawLine(transform.position, p.position);
        }
    }
#endif
}
