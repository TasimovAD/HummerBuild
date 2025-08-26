using UnityEngine;

/// <summary>
/// Простая настройка багажника машины используя StorageInventory вместо VehicleInventory.
/// Это обходит проблемы совместимости между ResourceDef и ResourceType.
/// </summary>
public class VehicleStorageSetup : MonoBehaviour
{
    [Header("Конфигурация")]
    [Tooltip("Максимальная вместимость багажника")]
    public int trunkCapacity = 20;
    
    [Tooltip("Максимальный вес багажника")]
    public float maxWeight = 200f;
    
    [Tooltip("Позиция багажника относительно машины")]
    public Vector3 trunkLocalPosition = new Vector3(0, 0.5f, -2f);
    
    [Tooltip("Склад для выгрузки")]
    public StorageInventory targetStorage;

    [Header("UI")]
    public GameObject trunkUIPrefab;
    [Tooltip("Конкретный Canvas для UI (если null - найдет первый)")]
    public Canvas targetCanvas;

    [Header("Отладка")]
    public bool setupOnStart = true;
    public bool showGizmos = true;

    // Созданные компоненты
    [System.NonSerialized] public StorageInventory vehicleStorage;
    [System.NonSerialized] public VehicleStorageInteraction trunkInteraction;
    [System.NonSerialized] public TransferZoneVehicleOnly transferZone;

    void Start()
    {
        if (setupOnStart)
        {
            SetupVehicleStorage();
        }
    }

    [ContextMenu("Setup Vehicle Storage")]
    public void SetupVehicleStorage()
    {
        Debug.Log($"[VehicleStorageSetup] Настройка багажника для машины: {gameObject.name}");

        // 1. Основной склад машины (вместо VehicleInventory)
        SetupMainStorage();

        // 2. Багажник с взаимодействием
        SetupTrunkComponents();

        // 3. Зона выгрузки
        SetupUnloadZone();

        // 4. UI
        SetupUI();

        Debug.Log("[VehicleStorageSetup] Настройка завершена!");
    }

    void SetupMainStorage()
    {
        vehicleStorage = GetComponent<StorageInventory>();
        if (!vehicleStorage)
        {
            vehicleStorage = gameObject.AddComponent<StorageInventory>();
            Debug.Log("[VehicleStorageSetup] Добавлен StorageInventory");
        }

        // Настройка лимитов
        vehicleStorage.Slots = trunkCapacity;
        vehicleStorage.MaxKg = maxWeight;
        vehicleStorage.ProviderId = $"{gameObject.name}_Trunk";

        Debug.Log($"[VehicleStorageSetup] Настроен склад с лимитом {trunkCapacity} слотов, {maxWeight} кг");
    }

    void SetupTrunkComponents()
    {
        // Создаем дочерний объект для багажника
        GameObject trunkGO = transform.Find("Trunk")?.gameObject;
        if (!trunkGO)
        {
            trunkGO = new GameObject("Trunk");
            trunkGO.transform.SetParent(transform);
            trunkGO.transform.localPosition = trunkLocalPosition;
            trunkGO.transform.localRotation = Quaternion.identity;
            Debug.Log("[VehicleStorageSetup] Создан объект Trunk");
        }

        // VehicleStorageInteraction
        trunkInteraction = trunkGO.GetComponent<VehicleStorageInteraction>();
        if (!trunkInteraction)
        {
            trunkInteraction = trunkGO.AddComponent<VehicleStorageInteraction>();
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

        Debug.Log("[VehicleStorageSetup] Настроены компоненты багажника");
    }

    void SetupUnloadZone()
    {
        // Создаем дочерний объект для зоны выгрузки
        GameObject unloadGO = transform.Find("UnloadZone")?.gameObject;
        if (!unloadGO)
        {
            unloadGO = new GameObject("UnloadZone");
            unloadGO.transform.SetParent(transform);
            unloadGO.transform.localPosition = new Vector3(0, 1f, -3f);
            Debug.Log("[VehicleStorageSetup] Создан объект UnloadZone");
        }

        // Для зоны выгрузки нужен другой компонент, поскольку TransferZoneVehicleOnly ожидает VehicleInventory
        var simpleZone = unloadGO.GetComponent<SimpleTransferZone>();
        if (!simpleZone)
        {
            simpleZone = unloadGO.AddComponent<SimpleTransferZone>();
        }

        simpleZone.fromStorage = vehicleStorage;
        simpleZone.toStorage = targetStorage;

        // Коллайдер для зоны
        Collider zoneCollider = unloadGO.GetComponent<Collider>();
        if (!zoneCollider)
        {
            var boxCollider = unloadGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(4f, 2f, 4f);
        }

        Debug.Log("[VehicleStorageSetup] Настроена зона выгрузки");
    }

    void SetupUI()
    {
        if (!trunkInteraction) return;

        if (trunkUIPrefab)
        {
            // Используем указанный Canvas или ищем RCCP_Canvas по имени
            Canvas canvas = targetCanvas;
            if (!canvas)
            {
                // Ищем именно RCCP_Canvas
                var allCanvases = FindObjectsOfType<Canvas>();
                foreach (var c in allCanvases)
                {
                    if (c.name == "RCCP_Canvas")
                    {
                        canvas = c;
                        break;
                    }
                }
                
                // Если не нашли RCCP_Canvas - используем первый
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
                Debug.Log($"[VehicleStorageSetup] UI создан на Canvas: {canvas.name}");
            }
            else
            {
                Debug.LogWarning("[VehicleStorageSetup] Не найден Canvas для UI");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Багажник
        Gizmos.color = Color.blue;
        Vector3 trunkPos = transform.TransformPoint(trunkLocalPosition);
        Gizmos.DrawWireCube(trunkPos, Vector3.one);
        Gizmos.DrawWireSphere(trunkPos, 2f);

        // Зона выгрузки
        Gizmos.color = Color.green;
        Vector3 unloadPos = transform.TransformPoint(new Vector3(0, 1f, -3f));
        Gizmos.DrawWireCube(unloadPos, new Vector3(4f, 2f, 4f));
    }
}

/// <summary>
/// Взаимодействие с багажником на основе StorageInventory
/// </summary>
public class VehicleStorageInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    public StorageInventory vehicleStorage;
    public GameObject interactionPanel;
    public UnityEngine.UI.Button loadButton;
    public UnityEngine.UI.Button unloadButton;
    public TMPro.TextMeshProUGUI hintText;
    
    [Header("Настройки")]
    public float interactionDistance = 3f;
    
    private PlayerCarryController playerCarry;
    private bool isPlayerNearby;
    
    void Start()
    {
        playerCarry = FindObjectOfType<PlayerCarryController>();
        
        if (loadButton)
            loadButton.onClick.AddListener(LoadToTrunk);
            
        if (unloadButton)
            unloadButton.onClick.AddListener(UnloadFromTrunk);
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
        }
    }

    void UpdateUI()
    {
        if (!isPlayerNearby || !interactionPanel || !interactionPanel.activeInHierarchy)
            return;

        bool playerHasItem = playerCarry && playerCarry.IsCarrying;
        bool trunkHasItems = HasItemsInTrunk();

        if (loadButton)
            loadButton.interactable = playerHasItem;

        if (unloadButton)
            unloadButton.interactable = trunkHasItems;

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

    public void LoadToTrunk()
    {
        if (!playerCarry || !vehicleStorage || !playerCarry.IsCarrying)
            return;

        var carriedProp = playerCarry.CurrentProp;
        var tag = carriedProp.GetComponentInChildren<CarryPropTag>();
        var resource = tag?.resource;

        if (!resource)
        {
            Debug.LogWarning("[VehicleStorageInteraction] Не удалось определить тип ресурса");
            return;
        }

        // Конвертируем ResourceDef в ScriptableObject для StorageInventory
        ScriptableObject resourceSO = resource as ScriptableObject;
        if (!resourceSO)
        {
            Debug.LogWarning($"[VehicleStorageInteraction] ResourceDef {resource.DisplayName} не является ScriptableObject");
            return;
        }

        int added = vehicleStorage.AddItem(resourceSO, 1);
        if (added > 0)
        {
            playerCarry.Detach();
            Destroy(carriedProp);
            Debug.Log($"[VehicleStorageInteraction] Загружено в багажник: {resource.DisplayName}");
        }
        else
        {
            Debug.LogWarning("[VehicleStorageInteraction] Багажник полон или ошибка");
        }
    }

    public void UnloadFromTrunk()
    {
        if (!playerCarry || !vehicleStorage || playerCarry.IsCarrying)
            return;

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

        if (!found) return;

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
                    Debug.Log($"[VehicleStorageInteraction] Выгружено из багажника: {found.DisplayName}");
                }
                else
                {
                    Destroy(prop);
                }
            }
        }
    }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}

/// <summary>
/// Простая зона передачи между двумя StorageInventory
/// </summary>
public class SimpleTransferZone : MonoBehaviour
{
    [Header("Передача")]
    public StorageInventory fromStorage;
    public StorageInventory toStorage;
    
    [Header("UI")]
    public GameObject panel;
    public UnityEngine.UI.Button transferButton;
    
    private StorageInventory currentStorage;

    void Start()
    {
        if (panel) panel.SetActive(false);
        
        if (transferButton)
        {
            transferButton.onClick.RemoveAllListeners();
            transferButton.onClick.AddListener(TransferAll);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var storage = other.GetComponentInParent<StorageInventory>();
        if (storage && storage == currentStorage)
        {
            currentStorage = null;
            if (panel) panel.SetActive(false);
            Debug.Log("[SimpleTransferZone] Машина покинула зону выгрузки");
        }
    }

    public void TransferAll()
    {
        if (!currentStorage || !toStorage)
        {
            Debug.LogWarning("[SimpleTransferZone] Нет склада для передачи");
            return;
        }

        int totalTransferred = 0;
        var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
        
        foreach (var resourceDef in allResourceDefs)
        {
            ScriptableObject resourceSO = resourceDef as ScriptableObject;
            if (!resourceSO) continue;

            int available = currentStorage.GetAmount(resourceSO);
            if (available <= 0) continue;

            // Передаем по одному, чтобы учесть лимиты целевого склада
            for (int i = 0; i < available; i++)
            {
                int removed = currentStorage.RemoveItem(resourceSO, 1);
                if (removed > 0)
                {
                    int added = toStorage.AddItem(resourceSO, 1);
                    if (added > 0)
                    {
                        totalTransferred++;
                    }
                    else
                    {
                        // Если не удалось добавить - возвращаем обратно
                        currentStorage.AddItem(resourceSO, 1);
                        Debug.LogWarning($"[SimpleTransferZone] Целевой склад полон для ресурса: {resourceDef.DisplayName}");
                        break; // Прекращаем передачу этого ресурса
                    }
                }
            }
        }

        if (totalTransferred > 0)
        {
            Debug.Log($"[SimpleTransferZone] Передано {totalTransferred} единиц ресурсов из {currentStorage.ProviderId} в {toStorage.ProviderId}");
        }
        else
        {
            Debug.Log("[SimpleTransferZone] Нечего передавать или целевой склад полон");
        }
    }

    void OnDrawGizmosSelected()
    {
        var collider = GetComponent<Collider>();
        if (collider)
        {
            Gizmos.color = currentStorage ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (collider is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }
}