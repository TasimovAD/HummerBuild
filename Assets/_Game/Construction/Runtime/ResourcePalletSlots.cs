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
    // слот → объект (так надёжнее, чем просто список)
    private readonly Dictionary<Transform, GameObject> _placed = new();

    void Awake()
    {
        if (AutoFindSlots) FindSlots();
    }

    /// Собирает детей SlotRoot с именами Slot_0, Slot_1...
    [ContextMenu("Find Slots")]
    public void FindSlots()
    {
        _slots.Clear();
        _placed.Clear();

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
        foreach (var kv in _placed)
            if (kv.Value) Destroy(kv.Value);
        _placed.Clear();
    }

    /// Полная перестройка палеты под нужное количество (инстанс по префабу)
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
            _placed[slot] = go;
        }
    }

    /// Удобная перегрузка, если хочешь вызвать Rebuild только с количеством
    public void Rebuild(int count)
    {
        Rebuild(count, Resource != null ? Resource.CarryProp : DefaultPrefab);
    }

    /// Положить УЖЕ СУЩЕСТВУЮЩИЙ объект в первый свободный слот
    public bool TryAdd(GameObject go)
    {
        if (!go) return false;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!_placed.TryGetValue(slot, out var cur) || cur == null)
            {
                go.transform.SetParent(slot, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                _placed[slot] = go;
                return true;
            }
        }
        return false; // нет свободных слотов
    }

    /// Взять 1 объект со слотов
    public GameObject Take()
    {
        foreach (var slot in _slots)
        {
            if (_placed.TryGetValue(slot, out var go) && go != null)
            {
                _placed[slot] = null;
                go.transform.SetParent(null, true);
                return go;
            }
        }
        return null;
    }
}
