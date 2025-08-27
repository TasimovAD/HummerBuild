using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Визуализация ресурсов в багажнике машины.
/// Аналогично ResourcePalletSlots, но для смешанных типов ресурсов.
/// </summary>
public class VehicleTrunkSlots : MonoBehaviour
{
    [Header("Настройки слотов")]
    [Tooltip("Корневой объект для поиска слотов")]
    public Transform SlotRoot;
    
    [Tooltip("Автоматически найти слоты Slot_0, Slot_1, ...")]
    public bool AutoFindSlots = true;
    
    [Tooltip("Максимальное количество слотов")]
    public int MaxSlots = 20;

    [Header("Визуал по умолчанию")]
    [Tooltip("Префаб для ресурсов без CarryProp")]
    public GameObject DefaultPrefab;
    
    [Header("Отладка")]
    public bool ShowDebugInfo = false;

    // Слоты для размещения ресурсов
    private readonly List<Transform> _slots = new List<Transform>();
    
    // Данные о том, какой ресурс в каком слоте
    private readonly Dictionary<Transform, SlotData> _slotData = new Dictionary<Transform, SlotData>();

    [System.Serializable]
    public class SlotData
    {
        public ResourceDef resource;
        public GameObject visualObject;
        public bool isEmpty => visualObject == null;
    }

    void Awake()
    {
        if (AutoFindSlots) FindSlots();
        InitializeSlots();
    }

    /// <summary>
    /// Поиск слотов в SlotRoot
    /// </summary>
    public void FindSlots()
    {
        _slots.Clear();

        if (SlotRoot == null)
        {
            Debug.LogWarning($"[VehicleTrunkSlots] SlotRoot не назначен у {name}");
            return;
        }

        // Собираем существующие слоты
        foreach (Transform child in SlotRoot)
        {
            if (child.name.StartsWith("Slot_"))
                _slots.Add(child);
        }

        // Создаем недостающие слоты если нужно
        CreateMissingSlots();
        
        // Сортируем слоты по номерам
        _slots.Sort((a, b) => {
            if (int.TryParse(a.name.Replace("Slot_", ""), out int numA) &&
                int.TryParse(b.name.Replace("Slot_", ""), out int numB))
                return numA.CompareTo(numB);
            return string.Compare(a.name, b.name);
        });

        if (ShowDebugInfo)
            Debug.Log($"[VehicleTrunkSlots] Найдено {_slots.Count} слотов в {name}");
    }

    /// <summary>
    /// Создание недостающих слотов
    /// </summary>
    void CreateMissingSlots()
    {
        for (int i = _slots.Count; i < MaxSlots; i++)
        {
            GameObject slotGO = new GameObject($"Slot_{i}");
            slotGO.transform.SetParent(SlotRoot);
            
            // Расположение слотов в сетке (можно настроить)
            Vector3 position = CalculateSlotPosition(i);
            slotGO.transform.localPosition = position;
            slotGO.transform.localRotation = Quaternion.identity;
            
            _slots.Add(slotGO.transform);
        }
    }

    /// <summary>
    /// Расчет позиции слота (сетка 4x5)
    /// </summary>
    Vector3 CalculateSlotPosition(int index)
    {
        int rows = 4;
        int x = index % rows;
        int z = index / rows;
        
        return new Vector3(
            x * 0.5f - (rows * 0.5f * 0.5f), // центрируем по X
            0f,
            z * 0.5f
        );
    }

    /// <summary>
    /// Инициализация слотов
    /// </summary>
    void InitializeSlots()
    {
        _slotData.Clear();
        foreach (var slot in _slots)
        {
            _slotData[slot] = new SlotData();
        }
    }

    /// <summary>
    /// Обновление визуализации по данным инвентаря
    /// </summary>
    public void UpdateVisualization(StorageInventory storage)
    {
        if (storage == null) return;

        // Собираем все ресурсы из инвентаря
        var allResources = Resources.FindObjectsOfTypeAll<ResourceDef>();
        var resourceCounts = new Dictionary<ResourceDef, int>();

        foreach (var res in allResources)
        {
            if (res is ScriptableObject so)
            {
                int count = storage.GetAmount(so);
                if (count > 0)
                    resourceCounts[res] = count;
            }
        }

        // Очищаем все слоты
        ClearAllSlots();

        // Заполняем слоты ресурсами
        int slotIndex = 0;
        foreach (var kvp in resourceCounts)
        {
            var resource = kvp.Key;
            int count = kvp.Value;

            for (int i = 0; i < count && slotIndex < _slots.Count; i++, slotIndex++)
            {
                CreateResourceInSlot(slotIndex, resource);
            }
        }

        if (ShowDebugInfo)
            Debug.Log($"[VehicleTrunkSlots] Обновлена визуализация: {slotIndex} предметов в багажнике");
    }

    /// <summary>
    /// Создание визуального ресурса в слоте
    /// </summary>
    void CreateResourceInSlot(int slotIndex, ResourceDef resource)
    {
        if (slotIndex >= _slots.Count) return;

        var slot = _slots[slotIndex];
        var slotData = _slotData[slot];

        // Определяем префаб
        GameObject prefab = resource.CarryProp ?? DefaultPrefab;
        if (!prefab)
        {
            if (ShowDebugInfo)
                Debug.LogWarning($"[VehicleTrunkSlots] Нет префаба для ресурса {resource.DisplayName}");
            return;
        }

        // Создаем визуальный объект
        var visualObj = Instantiate(prefab, slot.position, slot.rotation, slot);
        visualObj.name = $"{resource.Id}_{slotIndex}";
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;

        // Убираем физику
        if (visualObj.TryGetComponent<Rigidbody>(out var rb)) 
            Destroy(rb);
        
        foreach (var col in visualObj.GetComponentsInChildren<Collider>()) 
            col.enabled = false;

        // Добавляем тег ресурса
        var tag = visualObj.GetComponent<CarryPropTag>();
        if (!tag) tag = visualObj.AddComponent<CarryPropTag>();
        tag.resource = resource;

        // Сохраняем данные слота
        slotData.resource = resource;
        slotData.visualObject = visualObj;
    }

    /// <summary>
    /// Очистка всех слотов
    /// </summary>
    void ClearAllSlots()
    {
        foreach (var slot in _slots)
        {
            if (!slot) continue;

            var slotData = _slotData[slot];
            if (slotData.visualObject)
            {
                Destroy(slotData.visualObject);
                slotData.visualObject = null;
                slotData.resource = null;
            }
        }
    }

    /// <summary>
    /// Разместить готовый визуальный объект в свободном слоте
    /// </summary>
    public bool PlaceVisualObject(GameObject obj, ResourceDef resource)
    {
        Debug.Log($"[VehicleTrunkSlots] ===== PlaceVisualObject =====");
        Debug.Log($"[VehicleTrunkSlots] Объект: {(obj ? obj.name : "NULL")}");
        Debug.Log($"[VehicleTrunkSlots] Ресурс: {(resource ? resource.DisplayName : "NULL")}");
        
        if (!obj || !resource) 
        {
            Debug.LogError("[VehicleTrunkSlots] ❌ Объект или ресурс == null");
            return false;
        }

        Debug.Log($"[VehicleTrunkSlots] Всего слотов: {_slots.Count}");

        // Ищем свободный слот
        int checkedSlots = 0;
        foreach (var slot in _slots)
        {
            checkedSlots++;
            if (!slot)
            {
                Debug.LogWarning($"[VehicleTrunkSlots] ⚠️ Слот {checkedSlots} == null, пропускаем");
                continue;
            }

            if (!_slotData.ContainsKey(slot))
            {
                Debug.LogWarning($"[VehicleTrunkSlots] ⚠️ Нет данных для слота {checkedSlots}, пропускаем");
                continue;
            }

            var slotData = _slotData[slot];
            Debug.Log($"[VehicleTrunkSlots] Слот {checkedSlots}: {(slotData.isEmpty ? "свободен" : "занят")}");
            
            if (slotData.isEmpty)
            {
                Debug.Log($"[VehicleTrunkSlots] ✅ Найден свободный слот {checkedSlots}, размещаем объект");
                
                // Размещаем объект в слоте
                obj.transform.SetParent(slot, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                
                Debug.Log($"[VehicleTrunkSlots] Объект перемещен в слот: {slot.name}");

                // Убираем физику для стабильности
                if (obj.TryGetComponent<Rigidbody>(out var rb)) 
                {
                    Debug.Log("[VehicleTrunkSlots] Удаляем Rigidbody");
                    Destroy(rb);
                }
                
                var colliders = obj.GetComponentsInChildren<Collider>();
                Debug.Log($"[VehicleTrunkSlots] Отключаем {colliders.Length} коллайдеров");
                foreach (var col in colliders) 
                    col.enabled = false;

                // Гарантируем правильный тег ресурса
                var tag = obj.GetComponent<CarryPropTag>();
                if (!tag)
                {
                    Debug.Log("[VehicleTrunkSlots] Добавляем CarryPropTag");
                    tag = obj.AddComponent<CarryPropTag>();
                }
                tag.resource = resource;

                // Обновляем данные слота
                slotData.resource = resource;
                slotData.visualObject = obj;

                Debug.Log($"[VehicleTrunkSlots] ✅ Размещен реальный объект {resource.DisplayName} в слоте {checkedSlots}");
                return true;
            }
        }

        Debug.LogWarning($"[VehicleTrunkSlots] ❌ Нет свободных слотов для размещения {resource.DisplayName} (проверено {checkedSlots} слотов)");
        return false;
    }

    /// <summary>
    /// Взять один ресурс из багажника (для игрока)
    /// </summary>
    public GameObject TakeOne()
    {
        // Ищем первый занятый слот
        foreach (var slot in _slots)
        {
            var slotData = _slotData[slot];
            if (!slotData.isEmpty)
            {
                var obj = slotData.visualObject;
                
                // Открепляем от слота
                obj.transform.SetParent(null, true);
                
                // Очищаем данные слота
                slotData.visualObject = null;
                slotData.resource = null;

                if (ShowDebugInfo)
                    Debug.Log($"[VehicleTrunkSlots] Взят реальный объект из слота");

                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// Взять конкретный тип ресурса из багажника (приоритет)
    /// </summary>
    public GameObject TakeOneOfType(ResourceDef targetResource)
    {
        if (!targetResource) return TakeOne();

        // Сначала ищем нужный тип
        foreach (var slot in _slots)
        {
            var slotData = _slotData[slot];
            if (!slotData.isEmpty && slotData.resource == targetResource)
            {
                var obj = slotData.visualObject;
                
                // Открепляем от слота
                obj.transform.SetParent(null, true);
                
                // Очищаем данные слота
                slotData.visualObject = null;
                slotData.resource = null;

                if (ShowDebugInfo)
                    Debug.Log($"[VehicleTrunkSlots] Взят объект типа {targetResource.DisplayName} из слота");

                return obj;
            }
        }

        // Если не нашли нужный тип - берем любой
        return TakeOne();
    }

    /// <summary>
    /// Получить количество визуально отображаемых ресурсов
    /// </summary>
    public int VisualCount
    {
        get
        {
            return _slotData.Values.Count(data => !data.isEmpty);
        }
    }

    /// <summary>
    /// Получить список всех ресурсов в багажнике
    /// </summary>
    public Dictionary<ResourceDef, int> GetResourceCounts()
    {
        var counts = new Dictionary<ResourceDef, int>();
        
        foreach (var slotData in _slotData.Values)
        {
            if (!slotData.isEmpty)
            {
                if (counts.ContainsKey(slotData.resource))
                    counts[slotData.resource]++;
                else
                    counts[slotData.resource] = 1;
            }
        }

        return counts;
    }

    void OnDrawGizmosSelected()
    {
        if (_slots == null || _slots.Count == 0) return;

        // Показываем слоты
        foreach (var slot in _slots)
        {
            if (!slot) continue;

            var slotData = _slotData.ContainsKey(slot) ? _slotData[slot] : null;
            
            Gizmos.color = slotData != null && !slotData.isEmpty ? Color.green : Color.gray;
            Gizmos.DrawWireCube(slot.position, Vector3.one * 0.3f);
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("DEBUG/Update Test Visualization")]
    void DebugUpdateVisualization()
    {
        var storage = GetComponentInParent<StorageInventory>();
        if (storage)
        {
            UpdateVisualization(storage);
        }
        else
        {
            Debug.LogWarning("[VehicleTrunkSlots] Не найден StorageInventory для тестирования");
        }
    }

    [ContextMenu("DEBUG/Clear All Slots")]
    public void DebugClearAllSlots()
    {
        ClearAllSlots();
    }

    [ContextMenu("DEBUG/Print Slot Info")]
    void DebugPrintSlotInfo()
    {
        Debug.Log($"[VehicleTrunkSlots] Слотов: {_slots.Count}, Занято: {VisualCount}");
        
        var counts = GetResourceCounts();
        foreach (var kvp in counts)
        {
            Debug.Log($"  - {kvp.Key.DisplayName}: {kvp.Value}");
        }
    }
    #endif
}