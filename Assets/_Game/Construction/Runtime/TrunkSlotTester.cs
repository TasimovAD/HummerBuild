using UnityEngine;

/// <summary>
/// Тестер для системы слотов багажника.
/// Позволяет тестировать размещение ресурсов в конкретных слотах.
/// </summary>
public class TrunkSlotTester : MonoBehaviour
{
    [Header("Тестирование")]
    [Tooltip("Индекс слота для тестирования")]
    public int testSlotIndex = 0;
    
    [Tooltip("Ресурс для тестирования")]
    public ResourceDef testResource;
    
    [Tooltip("Префаб для тестирования")]
    public GameObject testPrefab;
    
    [Header("Автоматическое тестирование")]
    [Tooltip("Автоматически тестировать при старте")]
    public bool autoTestOnStart = false;
    
    [Tooltip("Интервал между тестами (секунды)")]
    public float testInterval = 2f;
    
    private VehicleTrunkSlots trunkSlots;
    private float lastTestTime;

    void Start()
    {
        // Находим компонент багажника
        trunkSlots = GetComponentInParent<VehicleTrunkSlots>();
        if (!trunkSlots)
        {
            trunkSlots = FindObjectOfType<VehicleTrunkSlots>();
        }
        
        if (!trunkSlots)
        {
            Debug.LogError("[TrunkSlotTester] Не найден VehicleTrunkSlots!");
            return;
        }
        
        if (autoTestOnStart)
        {
            Invoke(nameof(TestSlotPlacement), 1f);
        }
    }

    void Update()
    {
        if (autoTestOnStart && trunkSlots && Time.time - lastTestTime > testInterval)
        {
            TestSlotPlacement();
            lastTestTime = Time.time;
        }
    }

    [ContextMenu("Test Slot Placement")]
    public void TestSlotPlacement()
    {
        if (!trunkSlots)
        {
            Debug.LogError("[TrunkSlotTester] Не найден VehicleTrunkSlots!");
            return;
        }

        if (!testResource)
        {
            Debug.LogWarning("[TrunkSlotTester] Не назначен testResource!");
            return;
        }

        if (!testPrefab)
        {
            Debug.LogWarning("[TrunkSlotTester] Не назначен testPrefab!");
            return;
        }

        // Создаем тестовый объект
        GameObject testObj = Instantiate(testPrefab);
        testObj.name = $"TestResource_{testSlotIndex}";
        
        // Пытаемся разместить в указанном слоте
        bool success = trunkSlots.PlaceResourceInSpecificSlot(testObj, testResource, testSlotIndex);
        
        if (success)
        {
            Debug.Log($"[TrunkSlotTester] ✅ Ресурс {testResource.DisplayName} успешно размещен в слоте {testSlotIndex}");
        }
        else
        {
            Debug.LogError($"[TrunkSlotTester] ❌ Не удалось разместить ресурс {testResource.DisplayName} в слоте {testSlotIndex}");
            // Уничтожаем объект если не удалось разместить
            Destroy(testObj);
        }
    }

    [ContextMenu("Clear All Slots")]
    public void ClearAllSlots()
    {
        if (trunkSlots)
        {
            #if UNITY_EDITOR
            trunkSlots.DebugClearAllSlots();
            #else
            trunkSlots.ClearAllSlots();
            #endif
            Debug.Log("[TrunkSlotTester] Все слоты очищены");
        }
    }

    [ContextMenu("Print Slot Info")]
    public void PrintSlotInfo()
    {
        if (trunkSlots)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            trunkSlots.DebugPrintSlotInfo();
            #else
            Debug.Log($"[TrunkSlotTester] Слотов: {trunkSlots.SlotCount}");
            #endif
        }
    }

    [ContextMenu("Test Random Placement")]
    public void TestRandomPlacement()
    {
        if (!trunkSlots || !testResource || !testPrefab)
        {
            Debug.LogWarning("[TrunkSlotTester] Не все параметры настроены для тестирования!");
            return;
        }

        // Создаем тестовый объект
        GameObject testObj = Instantiate(testPrefab);
        testObj.name = $"TestResource_Random";
        
        // Пытаемся разместить в случайном слоте
        bool success = trunkSlots.PlaceVisualObject(testObj, testResource);
        
        if (success)
        {
            Debug.Log($"[TrunkSlotTester] ✅ Ресурс {testResource.DisplayName} успешно размещен в случайном слоте");
        }
        else
        {
            Debug.LogError($"[TrunkSlotTester] ❌ Не удалось разместить ресурс {testResource.DisplayName} - нет свободных слотов");
            Destroy(testObj);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!trunkSlots) return;
        
        // Показываем тестируемый слот
        var slotData = trunkSlots.GetSlotData(testSlotIndex);
        if (slotData != null)
        {
            Gizmos.color = slotData.isEmpty ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2, 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
                $"Тест слота {testSlotIndex}\nРесурс: {testResource?.DisplayName ?? "Не назначен"}\nСтатус: {(slotData.isEmpty ? "Свободен" : "Занят")}");
            #endif
        }
    }
}
