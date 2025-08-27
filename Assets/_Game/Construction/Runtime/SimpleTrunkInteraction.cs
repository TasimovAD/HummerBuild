using UnityEngine;
using System.Collections;

/// <summary>
/// Простое взаимодействие с багажником - ТОЛЬКО ручное, без зон выгрузки + визуализация
/// </summary>
public class SimpleTrunkInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    public StorageInventory vehicleStorage;
    public VehicleTrunkSlots trunkSlots;
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
        Debug.Log("[SimpleTrunkInteraction] ===== ИНИЦИАЛИЗАЦИЯ =====");
        
        playerCarry = FindObjectOfType<PlayerCarryController>();
        Debug.Log($"[SimpleTrunkInteraction] PlayerCarryController: {(playerCarry ? "найден" : "НЕ НАЙДЕН")}");
        
        Debug.Log($"[SimpleTrunkInteraction] vehicleStorage: {(vehicleStorage ? vehicleStorage.ProviderId : "NULL")}");
        Debug.Log($"[SimpleTrunkInteraction] trunkSlots: {(trunkSlots ? "найден" : "NULL")}");
        Debug.Log($"[SimpleTrunkInteraction] interactionPanel: {(interactionPanel ? interactionPanel.name : "NULL")}");

        // Отложим привязку кнопок до следующего кадра, чтобы UI успел создаться
        StartCoroutine(DelayedButtonBinding());

        Debug.Log($"[SimpleTrunkInteraction] Инициализация завершена для {gameObject.name}");
    }

    /// <summary>
    /// Отложенная привязка кнопок после создания UI
    /// </summary>
    IEnumerator DelayedButtonBinding()
    {
        // Ждем один кадр, чтобы SimpleTrunkOnly успел создать UI
        yield return null;

        Debug.Log("[SimpleTrunkInteraction] ===== ОТЛОЖЕННАЯ ПРИВЯЗКА КНОПОК =====");
        Debug.Log($"[SimpleTrunkInteraction] loadButton: {(loadButton ? loadButton.name : "NULL")}");
        Debug.Log($"[SimpleTrunkInteraction] unloadButton: {(unloadButton ? unloadButton.name : "NULL")}");
        
        if (loadButton)
        {
            Debug.Log("[SimpleTrunkInteraction] Привязываем LoadToTrunk к loadButton");
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadToTrunk);
            loadButton.onClick.AddListener(() => Debug.Log("[SimpleTrunkInteraction] 🔥 КНОПКА ЗАГРУЗКИ НАЖАТА!"));
        }
        else
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ loadButton == NULL после задержки!");
        }
            
        if (unloadButton)
        {
            Debug.Log("[SimpleTrunkInteraction] Привязываем UnloadFromTrunk к unloadButton");
            unloadButton.onClick.RemoveAllListeners();
            unloadButton.onClick.AddListener(UnloadFromTrunk);
            unloadButton.onClick.AddListener(() => Debug.Log("[SimpleTrunkInteraction] 🔥 КНОПКА ВЫГРУЗКИ НАЖАТА!"));
        }
        else
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ unloadButton == NULL после задержки!");
        }
    }

    /// <summary>
    /// Принудительная переподключение кнопок (для вызова извне)
    /// </summary>
    public void RebindButtons()
    {
        Debug.Log("[SimpleTrunkInteraction] ===== ПРИНУДИТЕЛЬНОЕ ПЕРЕПОДКЛЮЧЕНИЕ =====");
        
        if (loadButton)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadToTrunk);
            loadButton.onClick.AddListener(() => Debug.Log("[SimpleTrunkInteraction] 🔥 КНОПКА ЗАГРУЗКИ НАЖАТА!"));
            Debug.Log("[SimpleTrunkInteraction] ✅ Кнопка загрузки переподключена");
        }
        
        if (unloadButton)
        {
            unloadButton.onClick.RemoveAllListeners();
            unloadButton.onClick.AddListener(UnloadFromTrunk);
            unloadButton.onClick.AddListener(() => Debug.Log("[SimpleTrunkInteraction] 🔥 КНОПКА ВЫГРУЗКИ НАЖАТА!"));
            Debug.Log("[SimpleTrunkInteraction] ✅ Кнопка выгрузки переподключена");
        }
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
            Debug.Log($"[SimpleTrunkInteraction] Игрок {(isPlayerNearby ? "подошел" : "отошел")} к багажнику");
            Debug.Log($"[SimpleTrunkInteraction] interactionPanel: {(interactionPanel ? interactionPanel.name : "NULL")}");
            
            if (interactionPanel)
            {
                interactionPanel.SetActive(isPlayerNearby);
                Debug.Log($"[SimpleTrunkInteraction] UI панель {(isPlayerNearby ? "показана" : "скрыта")}");
            }
            else
            {
                Debug.LogError("[SimpleTrunkInteraction] ❌ interactionPanel == NULL, UI не может быть показан!");
            }
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
    /// Загрузить ресурс из рук в багажник (используем тот же объект)
    /// </summary>
    public void LoadToTrunk()
    {
        Debug.Log("[SimpleTrunkInteraction] ===== НАЧАЛО LoadToTrunk =====");
        
        if (!playerCarry)
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ playerCarry == null");
            return;
        }
        
        if (!vehicleStorage)
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ vehicleStorage == null");
            return;
        }
        
        if (!playerCarry.IsCarrying)
        {
            Debug.LogWarning("[SimpleTrunkInteraction] ❌ Игрок ничего не несет");
            return;
        }

        var carriedProp = playerCarry.CurrentProp;
        Debug.Log($"[SimpleTrunkInteraction] Carried Prop: {(carriedProp ? carriedProp.name : "NULL")}");
        
        if (!carriedProp)
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ CurrentProp == null, но IsCarrying == true");
            return;
        }

        var tag = carriedProp.GetComponentInChildren<CarryPropTag>();
        Debug.Log($"[SimpleTrunkInteraction] CarryPropTag: {(tag ? "найден" : "НЕ НАЙДЕН")}");
        
        var resource = tag?.resource;
        Debug.Log($"[SimpleTrunkInteraction] Resource: {(resource ? resource.DisplayName : "NULL")}");

        if (!resource)
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ Не удалось определить тип ресурса");
            return;
        }

        // Конвертируем ResourceDef в ScriptableObject для StorageInventory
        ScriptableObject resourceSO = resource as ScriptableObject;
        Debug.Log($"[SimpleTrunkInteraction] ResourceDef as ScriptableObject: {(resourceSO ? "успешно" : "ОШИБКА")}");
        
        if (!resourceSO)
        {
            Debug.LogError($"[SimpleTrunkInteraction] ❌ ResourceDef {resource.DisplayName} не является ScriptableObject");
            return;
        }

        Debug.Log($"[SimpleTrunkInteraction] Пытаемся добавить в склад: {resourceSO.name}");
        int added = vehicleStorage.AddItem(resourceSO, 1);
        Debug.Log($"[SimpleTrunkInteraction] Добавлено в склад: {added}");
        
        if (added > 0)
        {
            Debug.Log("[SimpleTrunkInteraction] ✅ Успешно добавлено в склад, отцепляем от игрока");
            
            // ВАЖНО: Не уничтожаем объект, а передаем его в багажник для визуализации
            playerCarry.Detach();
            Debug.Log("[SimpleTrunkInteraction] Игрок отцеплен от объекта");
            
            // Добавляем визуально в багажник тот же объект, который нес игрок
            if (trunkSlots)
            {
                Debug.Log("[SimpleTrunkInteraction] Пытаемся разместить объект в слоте...");
                bool placed = trunkSlots.PlaceVisualObject(carriedProp, resource);
                Debug.Log($"[SimpleTrunkInteraction] Размещение в слоте: {(placed ? "УСПЕШНО" : "НЕУДАЧНО")}");
                
                if (!placed)
                {
                    Debug.LogWarning("[SimpleTrunkInteraction] ⚠️ Не удалось разместить в слоте, уничтожаем объект");
                    Destroy(carriedProp);
                }
            }
            else
            {
                Debug.LogWarning("[SimpleTrunkInteraction] ⚠️ trunkSlots == null, уничтожаем объект");
                Destroy(carriedProp);
            }
            
            Debug.Log($"[SimpleTrunkInteraction] ✅ Загружен реальный объект в багажник: {resource.DisplayName}");
        }
        else
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ Багажник полон или ошибка добавления в склад");
        }
        
        Debug.Log("[SimpleTrunkInteraction] ===== КОНЕЦ LoadToTrunk =====");
    }

    /// <summary>
    /// Выгрузить ресурс из багажника в руки (приоритет реального объекта из слота)
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

        // Сначала пробуем взять реальный объект из визуальных слотов
        GameObject prop = null;
        ResourceDef foundResource = null;

        if (trunkSlots)
        {
            prop = trunkSlots.TakeOne();
            if (prop)
            {
                var tag = prop.GetComponent<CarryPropTag>();
                foundResource = tag?.resource;
            }
        }

        // Если не нашли визуальный объект - найдем любой ресурс в инвентаре и создадим новый
        if (!prop || !foundResource)
        {
            var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
            
            foreach (var res in allResourceDefs)
            {
                ScriptableObject resourceSO = res as ScriptableObject;
                if (resourceSO && vehicleStorage.GetAmount(resourceSO) > 0)
                {
                    foundResource = res;
                    break;
                }
            }

            if (!foundResource)
            {
                if (debugLogs)
                    Debug.LogWarning("[SimpleTrunkInteraction] Багажник пуст");
                return;
            }

            // Создаем новый объект только если не нашли визуальный
            if (foundResource.CarryProp)
            {
                prop = Instantiate(foundResource.CarryProp);
                var tag = prop.GetComponent<CarryPropTag>();
                if (!tag) tag = prop.AddComponent<CarryPropTag>();
                tag.resource = foundResource;

                if (debugLogs)
                    Debug.Log($"[SimpleTrunkInteraction] Создан новый объект для {foundResource.DisplayName}");
            }
            else
            {
                if (debugLogs)
                    Debug.LogWarning($"[SimpleTrunkInteraction] У ресурса {foundResource.DisplayName} нет CarryProp");
                return;
            }
        }
        else
        {
            if (debugLogs)
                Debug.Log($"[SimpleTrunkInteraction] Взят реальный объект {foundResource.DisplayName} из слота");
        }

        // Удаляем из инвентаря
        ScriptableObject foundSO = foundResource as ScriptableObject;
        int removed = vehicleStorage.RemoveItem(foundSO, 1);
        
        if (removed > 0 && prop)
        {
            if (playerCarry.Attach(prop))
            {
                if (debugLogs)
                    Debug.Log($"[SimpleTrunkInteraction] ✅ Выгружен из багажника: {foundResource.DisplayName}");
            }
            else
            {
                Destroy(prop);
                if (debugLogs)
                    Debug.LogError("[SimpleTrunkInteraction] ❌ Не удалось прикрепить к игроку");
            }
        }
        else if (prop)
        {
            Destroy(prop);
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

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        if (trunkSlots)
        {
            var visualCounts = trunkSlots.GetResourceCounts();
            int totalVisual = trunkSlots.VisualCount;
            
            sb.AppendLine($"Визуальные объекты ({totalVisual}):");
            foreach (var kvp in visualCounts)
            {
                sb.AppendLine($"  {kvp.Key.DisplayName}: {kvp.Value} шт");
            }
        }

        var allResourceDefs = Resources.FindObjectsOfTypeAll<ResourceDef>();
        int totalInStorage = 0;
        sb.AppendLine("Данные склада:");
        
        foreach (var res in allResourceDefs)
        {
            ScriptableObject resourceSO = res as ScriptableObject;
            if (resourceSO)
            {
                int amount = vehicleStorage.GetAmount(resourceSO);
                if (amount > 0)
                {
                    sb.AppendLine($"  {res.DisplayName}: {amount} шт");
                    totalInStorage += amount;
                }
            }
        }

        return totalInStorage > 0 ? sb.ToString() : "Багажник пуст";
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

    [ContextMenu("DEBUG/Force Update Visualization")]
    void DebugUpdateVisualization()
    {
        if (trunkSlots && vehicleStorage)
        {
            trunkSlots.UpdateVisualization(vehicleStorage);
            Debug.Log("[SimpleTrunkInteraction] Визуализация принудительно обновлена");
        }
    }

    [ContextMenu("DEBUG/Test Load To Trunk")]
    void DebugTestLoadToTrunk()
    {
        Debug.Log("[SimpleTrunkInteraction] 🧪 ТЕСТОВЫЙ ВЫЗОВ LoadToTrunk()");
        LoadToTrunk();
    }

    [ContextMenu("DEBUG/Test Button Click")]
    void DebugTestButtonClick()
    {
        Debug.Log("[SimpleTrunkInteraction] 🧪 Имитация нажатия кнопки");
        if (loadButton)
        {
            Debug.Log("[SimpleTrunkInteraction] Кнопка найдена, вызываем onClick");
            loadButton.onClick.Invoke();
        }
        else
        {
            Debug.LogError("[SimpleTrunkInteraction] ❌ loadButton == null");
        }
    }
    #endif
}