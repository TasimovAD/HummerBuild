using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Визуализация ресурсов в багажнике машины с ручной настройкой позиций слотов.
/// Позволяет точно указать, где должен находиться каждый ресурс.
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

    [Header("Ручная настройка слотов")]
    [Tooltip("Включить ручную настройку позиций слотов")]
    public bool ManualSlotPositions = true;
    
    [Tooltip("Позиции слотов (если ManualSlotPositions = true)")]
    public List<SlotPosition> CustomSlotPositions = new List<SlotPosition>();

    [Header("Визуал по умолчанию")]
    [Tooltip("Префаб для ресурсов без CarryProp")]
    public GameObject DefaultPrefab;
    
    [Header("Физика слотов")]
    [Tooltip("Добавить коллайдеры к слотам для физического взаимодействия")]
    public bool AddSlotColliders = true;
    
    [Tooltip("Размер коллайдера слота")]
    public Vector3 SlotColliderSize = new Vector3(0.4f, 0.4f, 0.4f);
    
    [Header("Отладка")]
    public bool ShowDebugInfo = false;

    // Слоты для размещения ресурсов
    private readonly List<Transform> _slots = new List<Transform>();
    
    // Данные о том, какой ресурс в каком слоте
    private readonly Dictionary<Transform, SlotData> _slotData = new Dictionary<Transform, SlotData>();

    [System.Serializable]
    public class SlotPosition
    {
        [Tooltip("Название слота")]
        public string slotName = "Slot";
        
        [Tooltip("Локальная позиция относительно корня багажника")]
        public Vector3 localPosition = Vector3.zero;
        
        [Tooltip("Локальный поворот")]
        public Vector3 localRotation = Vector3.zero;
        
        [Tooltip("Размер слота для коллайдера")]
        public Vector3 size = new Vector3(0.4f, 0.4f, 0.4f);
        
        [Tooltip("Цвет слота в редакторе")]
        public Color gizmoColor = Color.blue;
    }

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

        if (ManualSlotPositions && CustomSlotPositions.Count > 0)
        {
            // Создаем слоты по ручным настройкам
            CreateManualSlots();
        }
        else
        {
            // Собираем существующие слоты
            foreach (Transform child in SlotRoot)
            {
                if (child.name.StartsWith("Slot_"))
                    _slots.Add(child);
            }

            // Создаем недостающие слоты если нужно
            CreateMissingSlots();
        }
        
        // Сортируем слоты по номерам
        _slots.Sort((a, b) => {
            if (int.TryParse(a.name.Replace("Slot_", ""), out int numA) &&
                int.TryParse(b.name.Replace("Slot_", ""), out int numB))
                return numA.CompareTo(numB);
            return string.Compare(a.name, b.name);
        });

        // Добавляем коллайдеры к слотам
        if (AddSlotColliders)
        {
            AddCollidersToSlots();
        }

        if (ShowDebugInfo)
            Debug.Log($"[VehicleTrunkSlots] Найдено {_slots.Count} слотов в {name}");
    }

    /// <summary>
    /// Создание слотов по ручным настройкам
    /// </summary>
    void CreateManualSlots()
    {
        for (int i = 0; i < CustomSlotPositions.Count; i++)
        {
            var slotPos = CustomSlotPositions[i];
            
            // Проверяем, существует ли уже слот с таким именем
            Transform existingSlot = SlotRoot.Find(slotPos.slotName);
            if (existingSlot)
            {
                _slots.Add(existingSlot);
                continue;
            }

            // Создаем новый слот
            GameObject slotGO = new GameObject(slotPos.slotName);
            slotGO.transform.SetParent(SlotRoot);
            slotGO.transform.localPosition = slotPos.localPosition;
            slotGO.transform.localRotation = Quaternion.Euler(slotPos.localRotation);
            
            _slots.Add(slotGO.transform);
        }
    }

    /// <summary>
    /// Создание недостающих слотов (для обратной совместимости)
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
    /// Расчет позиции слота (сетка 4x5) - для обратной совместимости
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
    /// Добавление коллайдеров к слотам
    /// </summary>
    void AddCollidersToSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (!slot) continue;

            // Убираем существующие коллайдеры
            var existingCollider = slot.GetComponent<Collider>();
            if (existingCollider)
                DestroyImmediate(existingCollider);

            // Добавляем новый коллайдер
            var boxCollider = slot.gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            
            // Используем размер из настроек или по умолчанию
            if (ManualSlotPositions && i < CustomSlotPositions.Count)
            {
                boxCollider.size = CustomSlotPositions[i].size;
            }
            else
            {
                boxCollider.size = SlotColliderSize;
            }

            // Добавляем компонент для отображения информации о слоте
            var slotInfo = slot.GetComponent<TrunkSlotInfo>();
            if (!slotInfo)
                slotInfo = slot.gameObject.AddComponent<TrunkSlotInfo>();
            
            slotInfo.slotIndex = i;
            slotInfo.slotName = slot.name;
        }
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
    public void ClearAllSlots()
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
    /// Разместить ресурс в конкретном слоте по индексу
    /// </summary>
    public bool PlaceResourceInSpecificSlot(GameObject obj, ResourceDef resource, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            Debug.LogError($"[VehicleTrunkSlots] Неверный индекс слота: {slotIndex}");
            return false;
        }

        var slot = _slots[slotIndex];
        var slotData = _slotData[slot];

        if (!slotData.isEmpty)
        {
            Debug.LogWarning($"[VehicleTrunkSlots] Слот {slotIndex} уже занят ресурсом {slotData.resource.DisplayName}");
            return false;
        }

        // Размещаем объект в указанном слоте
        obj.transform.SetParent(slot, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Убираем физику для стабильности
        if (obj.TryGetComponent<Rigidbody>(out var rb)) 
            Destroy(rb);
        
        foreach (var col in obj.GetComponentsInChildren<Collider>()) 
            col.enabled = false;

        // Гарантируем правильный тег ресурса
        var tag = obj.GetComponent<CarryPropTag>();
        if (!tag)
            tag = obj.AddComponent<CarryPropTag>();
        tag.resource = resource;

        // Обновляем данные слота
        slotData.resource = resource;
        slotData.visualObject = obj;

        Debug.Log($"[VehicleTrunkSlots] ✅ Размещен ресурс {resource.DisplayName} в слоте {slotIndex}");
        return true;
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

    /// <summary>
    /// Получить информацию о слоте по индексу
    /// </summary>
    public SlotData GetSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
            return null;
        
        var slot = _slots[slotIndex];
        return _slotData.ContainsKey(slot) ? _slotData[slot] : null;
    }

    /// <summary>
    /// Получить количество слотов
    /// </summary>
    public int SlotCount => _slots.Count;

    /// <summary>
    /// Получить количество визуально отображаемых ресурсов
    /// </summary>
    public int VisualCount
    {
        get
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _slotData.Values.Count(data => !data.isEmpty);
            #else
            // Оптимизированная версия для финальной сборки
            int count = 0;
            foreach (var slotData in _slotData.Values)
            {
                if (!slotData.isEmpty) count++;
            }
            return count;
            #endif
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_slots == null || _slots.Count == 0) return;

        // Показываем слоты
        foreach (var slot in _slots)
        {
            if (!slot) continue;

            var slotData = _slotData.ContainsKey(slot) ? _slotData[slot] : null;
            
            // Определяем цвет слота
            Color slotColor;
            if (slotData != null && !slotData.isEmpty)
            {
                slotColor = Color.green; // Занятый слот
            }
            else if (ManualSlotPositions)
            {
                // Ищем настройки для этого слота
                int slotIndex = _slots.IndexOf(slot);
                if (slotIndex >= 0 && slotIndex < CustomSlotPositions.Count)
                {
                    slotColor = CustomSlotPositions[slotIndex].gizmoColor;
                }
                else
                {
                    slotColor = Color.blue; // По умолчанию
                }
            }
            else
            {
                slotColor = Color.gray; // Автоматический слот
            }
            
            Gizmos.color = slotColor;
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
    public void DebugPrintSlotInfo()
    {
        Debug.Log($"[VehicleTrunkSlots] Слотов: {_slots.Count}, Занято: {VisualCount}");
        
        var counts = GetResourceCounts();
        foreach (var kvp in counts)
        {
            Debug.Log($"  - {kvp.Key.DisplayName}: {kvp.Value}");
        }
    }

    [ContextMenu("DEBUG/Regenerate Slots")]
    public void DebugRegenerateSlots()
    {
        FindSlots();
        InitializeSlots();
        Debug.Log("[VehicleTrunkSlots] Слоты пересозданы");
    }
    #endif
}

/// <summary>
/// Компонент для отображения информации о слоте в инспекторе
/// </summary>
public class TrunkSlotInfo : MonoBehaviour
{
    [Header("Информация о слоте")]
    public int slotIndex;
    public string slotName;
    
    [Header("Статус")]
    public bool isOccupied;
    public string resourceName;
    
    void Update()
    {
        // Обновляем информацию о слоте
        var trunkSlots = GetComponentInParent<VehicleTrunkSlots>();
        if (trunkSlots)
        {
            var slotData = trunkSlots.GetSlotData(slotIndex);
            if (slotData != null)
            {
                isOccupied = !slotData.isEmpty;
                resourceName = slotData.resource != null ? slotData.resource.DisplayName : "Пусто";
            }
        }
    }
}