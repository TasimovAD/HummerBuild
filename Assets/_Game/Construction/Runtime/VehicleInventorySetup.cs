using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Автоматическая настройка системы инвентаря для машины.
/// Создает все необходимые компоненты для работы с багажником.
/// </summary>
[System.Serializable]
public class VehicleInventoryConfig
{
    [Header("Багажник")]
    [Tooltip("Максимальная вместимость багажника")]
    public int trunkCapacity = 20;
    
    [Tooltip("Позиция багажника относительно машины")]
    public Vector3 trunkLocalPosition = new Vector3(0, 0.5f, -2f);
    
    [Header("Зона выгрузки")]
    [Tooltip("Размер зоны для автоматической выгрузки")]
    public Vector3 unloadZoneSize = new Vector3(4f, 2f, 4f);
    
    [Tooltip("Позиция зоны выгрузки относительно машины")]
    public Vector3 unloadZoneLocalPosition = new Vector3(0, 1f, -3f);
}

/// <summary>
/// Основной компонент для настройки машины с инвентарем
/// </summary>
public class VehicleInventorySetup : MonoBehaviour
{
    [Header("Конфигурация")]
    public VehicleInventoryConfig config = new VehicleInventoryConfig();
    
    [Header("UI префабы (назначить вручную)")]
    [Tooltip("Префаб UI панели для взаимодействия с багажником")]
    public GameObject trunkUIPrefab;
    
    [Tooltip("Склад для выгрузки (назначить вручную)")]
    public StorageInventory targetStorage;

    [Header("Отладка")]
    [Tooltip("Автоматически создать компоненты при старте")]
    public bool setupOnStart = true;
    
    [Tooltip("Показывать gizmos в редакторе")]
    public bool showGizmos = true;

    // Созданные компоненты (для отладки)
    [System.NonSerialized] public VehicleInventory vehicleInventory;
    [System.NonSerialized] public InventoryProviderAdapter trunkAdapter;
    [System.NonSerialized] public VehicleTrunkInteractable trunkInteractable;
    [System.NonSerialized] public VehicleTrunkPlayerInteraction trunkPlayerInteraction;
    [System.NonSerialized] public TransferZoneVehicleOnly transferZone;

    void Start()
    {
        if (setupOnStart)
        {
            SetupVehicleInventory();
        }
    }

    /// <summary>
    /// Создает и настраивает все компоненты для работы с инвентарем машины
    /// </summary>
    [ContextMenu("Setup Vehicle Inventory")]
    public void SetupVehicleInventory()
    {
        Debug.Log($"[VehicleInventorySetup] Настройка инвентаря для машины: {gameObject.name}");

        // 1. Основной инвентарь машины
        SetupMainInventory();

        // 2. Багажник с взаимодействием
        SetupTrunkComponents();

        // 3. Зона выгрузки
        SetupUnloadZone();

        // 4. UI
        SetupUI();

        Debug.Log("[VehicleInventorySetup] Настройка завершена!");
    }

    /// <summary>
    /// Настраивает основной инвентарь машины
    /// </summary>
    void SetupMainInventory()
    {
        // VehicleInventory (наследник InventoryProvider)
        vehicleInventory = GetComponent<VehicleInventory>();
        if (!vehicleInventory)
        {
            vehicleInventory = gameObject.AddComponent<VehicleInventory>();
            Debug.Log("[VehicleInventorySetup] Добавлен VehicleInventory");
        }

        // Настройка лимитов через инспектор поля
        vehicleInventory.slots = config.trunkCapacity;
        vehicleInventory.maxKg = config.trunkCapacity * 10f; // Примерный вес

        // Принудительно инициализируем инвентарь если нужно
        vehicleInventory.enabled = false;
        vehicleInventory.enabled = true; // Это вызовет OnEnable -> EnsureInit()

        Debug.Log($"[VehicleInventorySetup] Настроен инвентарь с лимитом {config.trunkCapacity} слотов");
    }

    /// <summary>
    /// Настраивает компоненты багажника
    /// </summary>
    void SetupTrunkComponents()
    {
        // Создаем дочерний объект для багажника
        GameObject trunkGO = transform.Find("Trunk")?.gameObject;
        if (!trunkGO)
        {
            trunkGO = new GameObject("Trunk");
            trunkGO.transform.SetParent(transform);
            trunkGO.transform.localPosition = config.trunkLocalPosition;
            trunkGO.transform.localRotation = Quaternion.identity;
            Debug.Log("[VehicleInventorySetup] Создан объект Trunk");
        }

        // InventoryProviderAdapter для багажника - связываем с VehicleInventory
        trunkAdapter = trunkGO.GetComponent<InventoryProviderAdapter>();
        if (!trunkAdapter)
        {
            trunkAdapter = trunkGO.AddComponent<InventoryProviderAdapter>();
        }
        
        // Настраиваем адаптер для работы с VehicleInventory
        trunkAdapter.provider = vehicleInventory; // Связываем с основным инвентарем
        trunkAdapter.keyMode = KeyMode.ResourceDef; // Используем ResourceDef как ключ
        trunkAdapter.getMethodName = "Get";         // Методы IInventory
        trunkAdapter.addMethodName = "Add";
        trunkAdapter.removeMethodName = "Remove";
        
        // VehicleTrunkInteractable
        trunkInteractable = trunkGO.GetComponent<VehicleTrunkInteractable>();
        if (!trunkInteractable)
        {
            trunkInteractable = trunkGO.AddComponent<VehicleTrunkInteractable>();
        }
        trunkInteractable.trunkInventory = trunkAdapter;

        // VehicleTrunkPlayerInteraction
        trunkPlayerInteraction = trunkGO.GetComponent<VehicleTrunkPlayerInteraction>();
        if (!trunkPlayerInteraction)
        {
            trunkPlayerInteraction = trunkGO.AddComponent<VehicleTrunkPlayerInteraction>();
        }
        trunkPlayerInteraction.trunkInteractable = trunkInteractable;

        // Коллайдер для обнаружения игрока (если нужен)
        Collider trunkCollider = trunkGO.GetComponent<Collider>();
        if (!trunkCollider)
        {
            var boxCollider = trunkGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * 2f; // Зона взаимодействия
        }

        Debug.Log("[VehicleInventorySetup] Настроены компоненты багажника");
    }

    /// <summary>
    /// Настраивает зону выгрузки
    /// </summary>
    void SetupUnloadZone()
    {
        // Создаем дочерний объект для зоны выгрузки
        GameObject unloadGO = transform.Find("UnloadZone")?.gameObject;
        if (!unloadGO)
        {
            unloadGO = new GameObject("UnloadZone");
            unloadGO.transform.SetParent(transform);
            unloadGO.transform.localPosition = config.unloadZoneLocalPosition;
            unloadGO.transform.localRotation = Quaternion.identity;
            Debug.Log("[VehicleInventorySetup] Создан объект UnloadZone");
        }

        // TransferZoneVehicleOnly
        transferZone = unloadGO.GetComponent<TransferZoneVehicleOnly>();
        if (!transferZone)
        {
            transferZone = unloadGO.AddComponent<TransferZoneVehicleOnly>();
        }

        // Настройка зоны
        transferZone.fromVehicle = vehicleInventory;
        transferZone.toStorage = targetStorage; // Назначается вручную в инспекторе
        transferZone.matchSpecificVehicle = false; // Принимать любую машину
        transferZone.type = null; // Выгружать все типы ресурсов
        transferZone.amount = 9999; // Выгружать все

        // Коллайдер для зоны
        Collider zoneCollider = unloadGO.GetComponent<Collider>();
        if (!zoneCollider)
        {
            var boxCollider = unloadGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = config.unloadZoneSize;
        }

        Debug.Log("[VehicleInventorySetup] Настроена зона выгрузки");
    }

    /// <summary>
    /// Настраивает UI
    /// </summary>
    void SetupUI()
    {
        if (!trunkPlayerInteraction) return;

        // Если UI префаб указан - создаем экземпляр
        if (trunkUIPrefab && !trunkPlayerInteraction.interactionPanel)
        {
            // Ищем Canvas в сцене
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas)
            {
                GameObject uiInstance = Instantiate(trunkUIPrefab, canvas.transform);
                trunkPlayerInteraction.interactionPanel = uiInstance;

                // Пытаемся найти кнопки автоматически
                Button[] buttons = uiInstance.GetComponentsInChildren<Button>();
                if (buttons.Length >= 2)
                {
                    trunkPlayerInteraction.loadButton = buttons[0];
                    trunkPlayerInteraction.unloadButton = buttons[1];
                }

                uiInstance.SetActive(false); // Скрываем до взаимодействия
                Debug.Log("[VehicleInventorySetup] UI создан и настроен");
            }
        }

        // Также настраиваем UI для зоны выгрузки
        if (transferZone && trunkUIPrefab)
        {
            // Здесь можно создать отдельную панель для зоны выгрузки
        }
    }

    /// <summary>
    /// Очищает все созданные компоненты
    /// </summary>
    [ContextMenu("Clean Up Components")]
    public void CleanUpComponents()
    {
        // Удаляем созданные дочерние объекты
        Transform trunk = transform.Find("Trunk");
        if (trunk) DestroyImmediate(trunk.gameObject);

        Transform unloadZone = transform.Find("UnloadZone");
        if (unloadZone) DestroyImmediate(unloadZone.gameObject);

        // Удаляем компоненты с основного объекта
        if (vehicleInventory) DestroyImmediate(vehicleInventory);

        Debug.Log("[VehicleInventorySetup] Компоненты очищены");
    }

    /// <summary>
    /// Gizmos для визуализации в редакторе
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Багажник
        Gizmos.color = Color.blue;
        Vector3 trunkPos = transform.TransformPoint(config.trunkLocalPosition);
        Gizmos.DrawWireCube(trunkPos, Vector3.one);
        Gizmos.DrawWireSphere(trunkPos, 2f); // Зона взаимодействия

        // Зона выгрузки
        Gizmos.color = Color.green;
        Vector3 unloadPos = transform.TransformPoint(config.unloadZoneLocalPosition);
        Gizmos.DrawWireCube(unloadPos, config.unloadZoneSize);
    }

    /// <summary>
    /// Валидация настроек
    /// </summary>
    void OnValidate()
    {
        if (config.trunkCapacity <= 0)
            config.trunkCapacity = 1;
    }
}