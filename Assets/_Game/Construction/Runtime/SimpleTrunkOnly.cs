using UnityEngine;

/// <summary>
/// Простая система багажника ТОЛЬКО с ручным взаимодействием игрока.
/// Без автоматической выгрузки и зон передачи + визуализация ресурсов.
/// </summary>
public class SimpleTrunkOnly : MonoBehaviour
{
    [Header("Конфигурация")]
    [Tooltip("Максимальная вместимость багажника")]
    public int trunkCapacity = 20;
    
    [Tooltip("Максимальный вес багажника")]
    public float maxWeight = 200f;
    
    [Tooltip("Позиция багажника относительно машины")]
    public Vector3 trunkLocalPosition = new Vector3(0, 0.5f, -2f);

    [Header("Визуал")]
    [Tooltip("Префаб по умолчанию для ресурсов без CarryProp")]
    public GameObject DefaultPrefab;

    [Header("UI")]
    public GameObject trunkUIPrefab;
    [Tooltip("Конкретный Canvas для UI (если null - найдет RCCP_Canvas)")]
    public Canvas targetCanvas;

    [Header("Отладка")]
    public bool setupOnStart = true;
    public bool showGizmos = true;

    // Созданные компоненты
    [System.NonSerialized] public StorageInventory vehicleStorage;
    [System.NonSerialized] public SimpleTrunkInteraction trunkInteraction;
    [System.NonSerialized] public VehicleTrunkSlots trunkSlots;

    void Start()
    {
        if (setupOnStart)
        {
            SetupTrunkOnly();
        }
    }

    [ContextMenu("Setup Trunk Only")]
    public void SetupTrunkOnly()
    {
        Debug.Log($"[SimpleTrunkOnly] Настройка багажника для машины: {gameObject.name}");

        // 1. Основной склад машины
        SetupVehicleStorage();

        // 2. Багажник с взаимодействием И визуализацией
        SetupTrunkInteraction();

        // 3. UI
        SetupUI();

        Debug.Log("[SimpleTrunkOnly] Настройка завершена! Только ручное взаимодействие + визуализация.");
    }

    void SetupVehicleStorage()
    {
        vehicleStorage = GetComponent<StorageInventory>();
        if (!vehicleStorage)
        {
            vehicleStorage = gameObject.AddComponent<StorageInventory>();
            Debug.Log("[SimpleTrunkOnly] Добавлен StorageInventory");
        }

        // Настройка лимитов
        vehicleStorage.Slots = trunkCapacity;
        vehicleStorage.MaxKg = maxWeight;
        vehicleStorage.ProviderId = $"{gameObject.name}_Trunk";

        Debug.Log($"[SimpleTrunkOnly] Настроен склад с лимитом {trunkCapacity} слотов, {maxWeight} кг");
    }

    void SetupTrunkInteraction()
    {
        // Создаем дочерний объект для багажника
        GameObject trunkGO = transform.Find("Trunk")?.gameObject;
        if (!trunkGO)
        {
            trunkGO = new GameObject("Trunk");
            trunkGO.transform.SetParent(transform);
            trunkGO.transform.localPosition = trunkLocalPosition;
            trunkGO.transform.localRotation = Quaternion.identity;
            Debug.Log("[SimpleTrunkOnly] Создан объект Trunk");
        }

        // Создаем корневой объект для слотов визуализации
        GameObject slotsRoot = trunkGO.transform.Find("TrunkSlots")?.gameObject;
        if (!slotsRoot)
        {
            slotsRoot = new GameObject("TrunkSlots");
            slotsRoot.transform.SetParent(trunkGO.transform);
            slotsRoot.transform.localPosition = Vector3.zero;
            slotsRoot.transform.localRotation = Quaternion.identity;
        }

        // Визуализация ресурсов в багажнике
        trunkSlots = trunkGO.GetComponent<VehicleTrunkSlots>();
        if (!trunkSlots)
        {
            trunkSlots = trunkGO.AddComponent<VehicleTrunkSlots>();
        }
        trunkSlots.SlotRoot = slotsRoot.transform;
        trunkSlots.MaxSlots = trunkCapacity;
        trunkSlots.DefaultPrefab = DefaultPrefab; // можно назначить в инспекторе

        // Взаимодействие только с игроком
        trunkInteraction = trunkGO.GetComponent<SimpleTrunkInteraction>();
        if (!trunkInteraction)
        {
            trunkInteraction = trunkGO.AddComponent<SimpleTrunkInteraction>();
        }
        trunkInteraction.vehicleStorage = vehicleStorage;
        trunkInteraction.trunkSlots = trunkSlots; // Связываем с визуализацией

        // Коллайдер для взаимодействия
        Collider trunkCollider = trunkGO.GetComponent<Collider>();
        if (!trunkCollider)
        {
            var boxCollider = trunkGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * 2f;
        }

        Debug.Log("[SimpleTrunkOnly] Настроено взаимодействие с багажником + визуализация");
    }

    void SetupUI()
    {
        if (!trunkInteraction) return;

        if (trunkUIPrefab)
        {
            // Ищем RCCP_Canvas или используем указанный
            Canvas canvas = targetCanvas;
            if (!canvas)
            {
                var allCanvases = FindObjectsOfType<Canvas>();
                foreach (var c in allCanvases)
                {
                    if (c.name == "RCCP_Canvas")
                    {
                        canvas = c;
                        break;
                    }
                }
                
                if (!canvas && allCanvases.Length > 0)
                    canvas = allCanvases[0];
            }

            if (canvas)
            {
                GameObject uiInstance = Instantiate(trunkUIPrefab, canvas.transform);
                
                // КРИТИЧЕСКИ ВАЖНО: привязываем UI СРАЗУ
                trunkInteraction.interactionPanel = uiInstance;

                var buttons = uiInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
                Debug.Log($"[SimpleTrunkOnly] Найдено кнопок в UI: {buttons.Length}");
                
                if (buttons.Length >= 2)
                {
                    trunkInteraction.loadButton = buttons[0];
                    trunkInteraction.unloadButton = buttons[1];
                    Debug.Log($"[SimpleTrunkOnly] Привязаны кнопки: {buttons[0].name}, {buttons[1].name}");
                }
                else
                {
                    Debug.LogWarning($"[SimpleTrunkOnly] В UI найдено только {buttons.Length} кнопок, ожидалось 2");
                }

                // ВАЖНО: Принудительно переподключаем кнопки после создания UI
                if (trunkInteraction.loadButton)
                {
                    trunkInteraction.loadButton.onClick.RemoveAllListeners();
                    trunkInteraction.loadButton.onClick.AddListener(trunkInteraction.LoadToTrunk);
                    Debug.Log("[SimpleTrunkOnly] Подключена кнопка загрузки");
                }

                if (trunkInteraction.unloadButton)
                {
                    trunkInteraction.unloadButton.onClick.RemoveAllListeners();
                    trunkInteraction.unloadButton.onClick.AddListener(trunkInteraction.UnloadFromTrunk);
                    Debug.Log("[SimpleTrunkOnly] Подключена кнопка выгрузки");
                }

                // Вызываем принудительное переподключение для гарантии
                trunkInteraction.RebindButtons();

                uiInstance.SetActive(false);
                Debug.Log($"[SimpleTrunkOnly] UI создан на Canvas: {canvas.name}");
            }
            else
            {
                Debug.LogWarning("[SimpleTrunkOnly] Не найден Canvas для UI");
            }
        }
        else
        {
            Debug.LogWarning("[SimpleTrunkOnly] trunkUIPrefab не назначен");
        }
    }

    [ContextMenu("Clean Up")]
    public void CleanUp()
    {
        // Удаляем созданные объекты
        Transform trunk = transform.Find("Trunk");
        if (trunk) 
        {
            // Очищаем визуальные слоты перед удалением
            var slots = trunk.GetComponent<VehicleTrunkSlots>();
            if (slots)
            {
                #if UNITY_EDITOR
                slots.DebugClearAllSlots();
                #endif
            }
            
            DestroyImmediate(trunk.gameObject);
        }

        if (vehicleStorage) DestroyImmediate(vehicleStorage);

        Debug.Log("[SimpleTrunkOnly] Компоненты очищены");
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Показываем только багажник
        Gizmos.color = Color.blue;
        Vector3 trunkPos = transform.TransformPoint(trunkLocalPosition);
        Gizmos.DrawWireCube(trunkPos, Vector3.one);
        Gizmos.DrawWireSphere(trunkPos, 2f); // Зона взаимодействия

        // Подпись
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(trunkPos + Vector3.up, "Багажник (только ручное взаимодействие + визуализация)");
        #endif
    }
}