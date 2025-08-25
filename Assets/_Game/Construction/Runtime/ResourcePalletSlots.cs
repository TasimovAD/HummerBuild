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

    void Awake()
    {
        if (AutoFindSlots) FindSlots();
        SyncFromHierarchy();
    }

    /// Собирает детей SlotRoot с именами Slot_...
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

    /// Синхронизация внутреннего состояния с фактическими детьми слотов
    public void SyncFromHierarchy()
    {
        // сейчас логика работает прямо по childCount, кэш не нужен
        // метод оставлен, чтобы явно вызывать «на всякий»
    }

    /// Полностью очистить палету (уничтожить всех детей слотов)
    public void ClearAll()
    {
        foreach (var slot in _slots)
        {
            if (!slot) continue;
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var ch = slot.GetChild(i);
                // ВАЖНО: проверяем, что объект все еще прикреплен к слоту
                // Если рабочий уже забрал объект, он будет откреплен от слота
                // и мы не должны его удалять
                if (ch && ch.parent == slot) Destroy(ch.gameObject);
            }
        }
    }

    /// Полная перестройка палеты под нужное количество (спавн из префаба)
    public void Rebuild(int count, GameObject prefab)
    {
        if (_slots.Count == 0)
        {
            Debug.LogWarning($"[ResourcePalletSlots] Нет ни одного слота у {name}");
            return;
        }

        // ВАЖНО: перед перестройкой проверяем, есть ли объекты, которые уже забрал рабочий
        // Если есть открепленные объекты, не удаляем их
        ClearAll();

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
        }
    }

    /// Удобная перегрузка (префаб берём из Resource или DefaultPrefab)
    public void Rebuild(int count)
    {
        Rebuild(count, Resource != null ? Resource.CarryProp : DefaultPrefab);
    }

    /// Взять 1 объект с палеты (любой занятой слот)
    public GameObject Take()
    {
        // пройдём слоты и найдём первый, у которого есть ребёнок
        foreach (var slot in _slots)
        {
            if (!slot) continue;
            if (slot.childCount > 0)
            {
                var go = slot.GetChild(0).gameObject;
                // ВАЖНО: открепляем объект от слота ПЕРЕД тем, как вернуть его
                // Это предотвращает конфликт с PalletGroupManager.RebuildAll
                go.transform.SetParent(null, true);
                return go;
            }
        }
        return null;
    }

    /// Положить готовый созданный объект в первый свободный слот.
    /// Возвращает true, если положили; объект становится child слота.
    public bool TryAdd(GameObject prop)
    {
        if (!prop) return false;

        // свободный слот = slot.childCount == 0
        foreach (var slot in _slots)
        {
            if (!slot) continue;
            if (slot.childCount == 0)
            {
                prop.transform.SetParent(slot, worldPositionStays: false);
                prop.transform.localPosition = Vector3.zero;
                prop.transform.localRotation = Quaternion.identity;
                return true;
            }
        }

        return false; // нет свободных слотов
    }

    /// Текущее число визуально лежащих объектов (по факту детей)
    public int VisualCount
    {
        get
        {
            int c = 0;
            foreach (var slot in _slots)
            {
                if (slot && slot.childCount > 0) c++;
            }
            return c;
        }
    }
}
