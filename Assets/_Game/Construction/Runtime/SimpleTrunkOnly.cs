using UnityEngine;

/// <summary>
/// Простая система багажника ТОЛЬКО с ручным взаимодействием игрока.
/// Без автоматической выгрузки и зон передачи.
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

        // 2. Багажник с взаимодействием (БЕЗ зон выгрузки)
        SetupTrunkInteraction();

        // 3. UI
        SetupUI();

        Debug.Log("[SimpleTrunkOnly] Настройка завершена! Только ручное взаимодействие.");
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

        // Взаимодействие только с игроком
        trunkInteraction = trunkGO.GetComponent<SimpleTrunkInteraction>();
        if (!trunkInteraction)
        {
            trunkInteraction = trunkGO.AddComponent<SimpleTrunkInteraction>();
        }
        trunkInteraction.vehicleStorage = vehicleStorage;

        // Коллайдер для взаимодействия
        Collider trunkCollider = trunkGO.GetComponent<Collider>();
        if (!trunkCollider)
        {
            var boxCollider = trunkGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = Vector3.one * 2f;
        }

        Debug.Log("[SimpleTrunkOnly] Настроено взаимодействие с багажником");
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
                trunkInteraction.interactionPanel = uiInstance;

                var buttons = uiInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
                if (buttons.Length >= 2)
                {
                    trunkInteraction.loadButton = buttons[0];
                    trunkInteraction.unloadButton = buttons[1];
                }

                uiInstance.SetActive(false);
                Debug.Log($"[SimpleTrunkOnly] UI создан на Canvas: {canvas.name}");
            }
            else
            {
                Debug.LogWarning("[SimpleTrunkOnly] Не найден Canvas для UI");
            }
        }
    }

    [ContextMenu("Clean Up")]
    public void CleanUp()
    {
        // Удаляем созданные объекты
        Transform trunk = transform.Find("Trunk");
        if (trunk) DestroyImmediate(trunk.gameObject);

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
        UnityEditor.Handles.Label(trunkPos + Vector3.up, "Багажник (только ручное взаимодействие)");
        #endif
    }
}

/// <summary>
/// Простое взаимодействие с багажником - ТОЛЬКО ручное, без зон выгрузки
/// </summary>
public class SimpleTrunkInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    public StorageInventory vehicleStorage;
    public GameObject interactionPanel;
    public UnityEngine.UI.Button loadButton;
    public UnityEngine.UI.Button unloadButton;
    public TMPro.TextMeshProUGUI hintText;
    
    [Header("Настройки")]
    public float interactionDistance = 3f;
    
    [Header("Отладка")]
    public bool debugLogs = true;
    
    private PlayerCarryController playerCarry;
    private bool isPlayerNearby;
    
    void Start()
    {
        playerCarry = FindObjectOfType<PlayerCarryController>();
        
        if (loadButton)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadToTrunk);
        }
            
        if (unloadButton)
        {
            unloadButton.onClick.RemoveAllListeners();
            unloadButton.onClick.AddListener(UnloadFromTrunk);
        }

        if (debugLogs)
            Debug.Log($"[SimpleTrunkInteraction] Инициализация для {gameObject.name}");
    }

    void Update()
    {
        CheckPlayerDistance();
        UpdateUI();
    }

    void CheckPlayerDistance()
    {
        if (!playerCarry) return;

        float distance = Vector3.Distance(transform.position, playerCarry.transform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distance <= interactionDistance;

        if (isPlayerNearby != wasNearby)
        {
            if (interactionPanel)
                interactionPanel.SetActive(isPlayerNearby);
                
            if (debugLogs)
                Debug.Log($"[SimpleTrunkInteraction] Игрок {(isPlayerNearby ? "подошел" : "отошел")}");
        }
    }

    void UpdateUI()
    {
        if (!isPlayerNearby || !interactionPanel || !interactionPanel.activeInHierarchy)
            return;

        bool playerHasItem = playerCarry && playerCarry.IsCarrying;
        bool trunkHasItems = HasItemsInTrunk();

        // Кнопки
        if (loadButton)
            loadButton.interactable = playerHasItem;

        if (unloadButton)
            unloadButton.interactable = trunkHasItems;

        // Подсказка
        if (hintText)
        {
            if (playerHasItem && trunkHasItems)
                hintText.text = "Можете загрузить или выгрузить";
            else if (playerHasItem)
                hintText.text = "Загрузить в багажник";
            else if (trunkHasItems)
                hintText.text = "Выгрузить из багажника";
            else
                hintText.text = "Багажник пуст, у вас нет предметов";
        }
    }

    /// <summary>
    /// Загрузить ресурс из рук в багажник
    /// </summary>
    public void LoadToTrunk()
    {
        if (!playerCarry || !vehicleStorage || !playerCarry.IsCarrying)
        {
            if (debugLogs)
                Debug.LogWarning("[SimpleTrunkInteraction] Не удается загрузить: нет предмета в руках");
            return;
        }

        var carriedProp = playerCarry.CurrentProp;
        var tag = carriedProp.GetComponentInChildren<CarryPropTag>();
        var resource = tag?.resource;

        if (!resource)
        {
            Debug.LogWarning("[SimpleTrunkInteraction] Не удалось определить тип ресурса");
            return;
        }

        // Конвертируем ResourceDef в ScriptableObject для StorageInventory
        ScriptableObject resourceSO = resource as ScriptableObject;
        if (!resourceSO)
        {
            Debug.LogWarning($"[SimpleTrunkInteraction] ResourceDef {resource.DisplayName} не является ScriptableObject");
            return;
        }

        int added = vehicleStorage.AddItem(resourceSO, 1);
        if (added > 0)
        {
            playerCarry.Detach();
            Destroy(carriedProp);
            
            if (debugLogs)
                Debug.Log($"[SimpleTrunkInteraction] ✅ Загружено в багажник: {resource.DisplayName}");
        }
        else
        {
            if (debugLogs)
                Debug.LogWarning("[SimpleTrunkInteraction] ❌ Багажник полон или ошибка");
        }
    }

    /// <summary>
    /// Выгрузить ресурс из багажника в руки
    /// </summary>
    public void UnloadFromTrunk()
    {
        if (!playerCarry || !vehicleStorage)
        {
            if (debugLogs)
                Debug.LogWarning("[SimpleTrunkInteraction] Нет ссылок для выгрузки");
            return;
        }

        if (playerCarry.IsCarrying)
        {
            if (debugLogs)
                Debug.LogWarning("[SimpleTrunkInteraction] У игрока уже есть предмет в руках");
            return;
        }

        // Ищем любой ресурс в багажнике
        var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
        ResourceDef found = null;
        
        foreach (var res in allResourceDefs)
        {
            ScriptableObject resourceSO = res as ScriptableObject;
            if (resourceSO && vehicleStorage.GetAmount(resourceSO) > 0)
            {
                found = res;
                break;
            }
        }

        if (!found)
        {
            if (debugLogs)
                Debug.LogWarning("[SimpleTrunkInteraction] Багажник пуст");
            return;
        }

        ScriptableObject foundSO = found as ScriptableObject;
        int removed = vehicleStorage.RemoveItem(foundSO, 1);
        
        if (removed > 0)
        {
            if (found.CarryProp)
            {
                var prop = Instantiate(found.CarryProp);
                var tag = prop.GetComponent<CarryPropTag>();
                if (!tag) tag = prop.AddComponent<CarryPropTag>();
                tag.resource = found;

                if (playerCarry.Attach(prop))
                {
                    if (debugLogs)
                        Debug.Log($"[SimpleTrunkInteraction] ✅ Выгружено из багажника: {found.DisplayName}");
                }
                else
                {
                    Destroy(prop);
                    if (debugLogs)
                        Debug.LogError("[SimpleTrunkInteraction] ❌ Не удалось прикрепить к игроку");
                }
            }
            else
            {
                if (debugLogs)
                    Debug.LogWarning($"[SimpleTrunkInteraction] У ресурса {found.DisplayName} нет CarryProp");
            }
        }
    }

    /// <summary>
    /// Проверка наличия ресурсов в багажнике
    /// </summary>
    bool HasItemsInTrunk()
    {
        if (!vehicleStorage) return false;

        var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
        foreach (var res in allResourceDefs)
        {
            ScriptableObject resourceSO = res as ScriptableObject;
            if (resourceSO && vehicleStorage.GetAmount(resourceSO) > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Получить статистику багажника для отладки
    /// </summary>
    public string GetTrunkStatus()
    {
        if (!vehicleStorage) return "Нет склада";

        var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
        int totalItems = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        foreach (var res in allResourceDefs)
        {
            ScriptableObject resourceSO = res as ScriptableObject;
            if (resourceSO)
            {
                int amount = vehicleStorage.GetAmount(resourceSO);
                if (amount > 0)
                {
                    sb.AppendLine($"  {res.DisplayName}: {amount}");
                    totalItems += amount;
                }
            }
        }

        return totalItems > 0 ? $"Багажник ({totalItems} предметов):\n{sb}" : "Багажник пуст";
    }

    void OnDrawGizmosSelected()
    {
        // Зона взаимодействия
        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Статус
        #if UNITY_EDITOR
        if (vehicleStorage)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, GetTrunkStatus());
        #endif
    }

    #if UNITY_EDITOR
    [ContextMenu("DEBUG/Print Trunk Status")]
    void DebugPrintStatus()
    {
        Debug.Log($"[SimpleTrunkInteraction] {GetTrunkStatus()}");
    }
    #endif
}