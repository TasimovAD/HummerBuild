using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PalletEntry {
    public ResourcePalletSlots pallet; // на палете в инспекторе задан Resource
}

public class PalletGroupManager : MonoBehaviour
{
    [Header("Источник данных (склад/буфер)")]
    public InventoryProviderAdapter inventory; // ← сюда перетащи BuildSite.Buffer (или склад)

    [Header("Палеты этой группы")]
    public List<PalletEntry> pallets = new();

    // быстрый доступ: какой ресурс → какие слоты
    private readonly Dictionary<ResourceDef, ResourcePalletSlots> _map = new();

    void Awake()
    {
        _map.Clear();
        foreach (var e in pallets)
        {
            if (e?.pallet == null || e.pallet.Resource == null) continue;
            if (!_map.ContainsKey(e.pallet.Resource))
                _map.Add(e.pallet.Resource, e.pallet);
        }

        if (inventory != null)
            inventory.OnChanged += OnInventoryChanged; // подпишемся на изменения
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnChanged -= OnInventoryChanged;
    }

    void Start()
    {
        RebuildAll();
    }

    // подпись совпадает с InventoryProviderAdapter.OnChanged(ResourceDef changedRes)
    void OnInventoryChanged(ResourceDef _)
    {
        RebuildAll();
    }

    /// Полностью перестроить все палеты по текущему количеству в инвентаре
    public void RebuildAll()
    {
        if (inventory == null) return;

        foreach (var kv in _map)
        {
            var res = kv.Key;
            var slots = kv.Value;

            int count = inventory.Get(res);
            var prefab = res != null ? res.CarryProp : null;
            if (slots != null)
            {
                // ВАЖНО: проверяем, что палета не содержит объекты, которые уже забрал рабочий
                // Если рабочий уже забрал объект, он будет откреплен от слота
                // и мы не должны его удалять при перестройке
                slots.Rebuild(count, prefab ?? slots.DefaultPrefab);
            }
        }
    }

    /// Забрать 1 визуальный проп из палеты (например, в руки рабочего)
    public GameObject Take(ResourceDef res)
    {
        if (res == null) return null;
        if (_map.TryGetValue(res, out var slots))
            return slots.Take();
        return null;
    }

    /// Получить GO палеты под конкретный ресурс (для навигации рабочего)
    public GameObject GetPalletFor(ResourceDef res)
    {
        if (res == null) return null;
        return _map.TryGetValue(res, out var slots) ? slots.gameObject : null;
    }

    public void ClearAll()
    {
        foreach (var e in pallets)
            e?.pallet?.ClearAll();
    }
}
