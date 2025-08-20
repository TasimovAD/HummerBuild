using UnityEngine;
using System.Collections.Generic;

/// Палета, где каждая единица ресурса кладётся строго в свой слот (Slot_0, Slot_1, …)
public class ResourcePalletSlots : MonoBehaviour
{
    [Header("Тип ресурса для этой палеты")]
    public ResourceDef Resource;

    [Header("Визуал по умолчанию (если у ресурса нет CarryProp)")]
    public GameObject DefaultPrefab;

    [Header("Где искать слоты (Slot_0, Slot_1, ...)")]
    public Transform SlotRoot;         // обычно это PalletSpawnRoot
    public bool AutoFindSlots = true;

    private readonly List<Transform> _slots = new();
    private readonly List<GameObject> _spawned = new();

    void Awake()
    {
        if (AutoFindSlots) FindSlots();
    }

    /// Собирает детей SlotRoot с именами Slot_0, Slot_1...
    public void FindSlots()
    {
        _slots.Clear();

        if (SlotRoot == null)
        {
            Debug.LogWarning($"[ResourcePalletSlots] SlotRoot пуст у {name}");
            return;
        }

        foreach (Transform t in SlotRoot)
        {
            if (t.name.StartsWith("Slot_"))
                _slots.Add(t);
        }
    }

    public void ClearAll()
    {
        foreach (var go in _spawned)
            if (go) Destroy(go);
        _spawned.Clear();
    }

    /// Полная перестройка палеты под нужное количество
    public void Rebuild(int count, GameObject prefab)
    {
        ClearAll();

        if (_slots.Count == 0)
        {
            Debug.LogWarning($"[ResourcePalletSlots] Нет ни одного слота у {name}");
            return;
        }

        var p = prefab ?? DefaultPrefab;
        if (p == null)
        {
            Debug.LogError($"[ResourcePalletSlots] Нет префаба для {name}");
            return;
        }

        int n = Mathf.Min(count, _slots.Count);
        for (int i = 0; i < n; i++)
        {
            var slot = _slots[i];
            var go = Instantiate(p, slot.position, slot.rotation, slot);
            go.name = $"{(Resource ? Resource.Id : p.name)}_{i}";
            _spawned.Add(go);
        }
    }

    /// Удобная перегрузка, если хочешь вызвать Rebuild только с количеством
    public void Rebuild(int count)
    {
        Rebuild(count, Resource != null ? Resource.CarryProp : DefaultPrefab);
    }

    /// Взять 1 объект со слотов
    public GameObject Take()
    {
        if (_spawned.Count == 0) return null;

        var go = _spawned[0];
        _spawned.RemoveAt(0);
        if (go) go.transform.SetParent(null);
        return go;
    }
}
