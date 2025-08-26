using UnityEngine;
using TMPro;

/// <summary>
/// Компонент для взаимодействия игрока с багажником машины.
/// Позволяет загружать/выгружать ресурсы из рук игрока.
/// </summary>
public class VehicleTrunkPlayerInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент багажника на этой же машине")]
    public VehicleTrunkInteractable trunkInteractable;
    
    [Tooltip("Контроллер переноса ресурсов игрока (найдется автоматически)")]
    public PlayerCarryController playerCarry;

    [Header("UI")]
    [Tooltip("Панель с кнопками взаимодействия")]
    public GameObject interactionPanel;
    
    [Tooltip("Кнопка для загрузки в багажник")]
    public UnityEngine.UI.Button loadButton;
    
    [Tooltip("Кнопка для выгрузки из багажника")]
    public UnityEngine.UI.Button unloadButton;
    
    [Tooltip("Текст подсказки")]
    public TextMeshProUGUI hintText;

    [Header("Настройки")]
    [Tooltip("Расстояние взаимодействия")]
    public float interactionDistance = 3f;
    
    [Tooltip("Тег игрока")]
    public string playerTag = "Player";

    // Приватные переменные
    private GameObject playerInRange;
    private bool isPlayerNearby;

    void Awake()
    {
        // Найдем компоненты автоматически, если не заданы
        if (!trunkInteractable)
            trunkInteractable = GetComponent<VehicleTrunkInteractable>();

        if (!playerCarry)
            playerCarry = FindObjectOfType<PlayerCarryController>();

        // Настройка UI
        if (interactionPanel) 
            interactionPanel.SetActive(false);

        // Привязка кнопок
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
    }

    void Update()
    {
        CheckPlayerProximity();
        UpdateUI();
    }

    /// <summary>
    /// Проверяет, находится ли игрок рядом с багажником
    /// </summary>
    void CheckPlayerProximity()
    {
        if (!playerCarry) return;

        float distance = Vector3.Distance(transform.position, playerCarry.transform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distance <= interactionDistance;

        // Если игрок только что подошел
        if (isPlayerNearby && !wasNearby)
        {
            playerInRange = playerCarry.gameObject;
            ShowInteractionPanel();
        }
        // Если игрок отошел
        else if (!isPlayerNearby && wasNearby)
        {
            playerInRange = null;
            HideInteractionPanel();
        }
    }

    /// <summary>
    /// Обновляет состояние UI кнопок и подсказок
    /// </summary>
    void UpdateUI()
    {
        if (!isPlayerNearby || !interactionPanel || !interactionPanel.activeInHierarchy)
            return;

        bool playerHasItem = playerCarry && playerCarry.IsCarrying;
        bool trunkHasItems = HasItemsInTrunk();

        // Кнопка загрузки активна, если у игрока есть предмет
        if (loadButton)
            loadButton.interactable = playerHasItem;

        // Кнопка выгрузки активна, если в багажнике есть предметы
        if (unloadButton)
            unloadButton.interactable = trunkHasItems;

        // Обновление подсказки
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
    /// Загружает предмет из рук игрока в багажник
    /// </summary>
    public void LoadToTrunk()
    {
        if (!playerCarry || !trunkInteractable || !playerCarry.IsCarrying)
        {
            Debug.LogWarning("[VehicleTrunkPlayerInteraction] Нельзя загрузить: нет предмета в руках");
            return;
        }

        GameObject carriedProp = playerCarry.CurrentProp;
        
        // Пытаемся положить в багажник
        if (trunkInteractable.TryPutOne(carriedProp))
        {
            // Успешно загружено - убираем из рук игрока
            playerCarry.Detach();
            Destroy(carriedProp);
            
            Debug.Log("[VehicleTrunkPlayerInteraction] Предмет загружен в багажник");
            
            // Можно добавить звуковой эффект или анимацию
            PlayLoadSound();
        }
        else
        {
            Debug.LogWarning("[VehicleTrunkPlayerInteraction] Не удалось загрузить в багажник (нет места?)");
        }
    }

    /// <summary>
    /// Выгружает предмет из багажника в руки игрока
    /// </summary>
    public void UnloadFromTrunk()
    {
        if (!playerCarry || !trunkInteractable)
        {
            Debug.LogWarning("[VehicleTrunkPlayerInteraction] Нет ссылок для выгрузки");
            return;
        }

        if (playerCarry.IsCarrying)
        {
            Debug.LogWarning("[VehicleTrunkPlayerInteraction] У игрока уже есть предмет в руках");
            return;
        }

        // Пытаемся взять из багажника
        if (trunkInteractable.TryTakeOne(out GameObject prop))
        {
            // Успешно взяли - передаем игроку
            if (playerCarry.Attach(prop))
            {
                Debug.Log("[VehicleTrunkPlayerInteraction] Предмет выгружен из багажника");
                
                // Можно добавить звуковой эффект или анимацию
                PlayUnloadSound();
            }
            else
            {
                // Если не удалось прикрепить к игроку - возвращаем в багажник
                Debug.LogError("[VehicleTrunkPlayerInteraction] Не удалось прикрепить предмет к игроку");
                Destroy(prop); // В идеале здесь нужно вернуть в багажник
            }
        }
        else
        {
            Debug.LogWarning("[VehicleTrunkPlayerInteraction] Багажник пуст или ошибка выгрузки");
        }
    }

    /// <summary>
    /// Проверяет, есть ли предметы в багажнике
    /// </summary>
    bool HasItemsInTrunk()
    {
        if (!trunkInteractable || !trunkInteractable.trunkInventory)
            return false;

        // Проверяем все типы ресурсов через адаптер
        var allResources = Resources.FindObjectsOfTypeAll<ResourceDef>();
        foreach (var res in allResources)
        {
            if (trunkInteractable.trunkInventory.Get(res) > 0)
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Показывает панель взаимодействия
    /// </summary>
    void ShowInteractionPanel()
    {
        if (interactionPanel)
            interactionPanel.SetActive(true);
    }

    /// <summary>
    /// Скрывает панель взаимодействия
    /// </summary>
    void HideInteractionPanel()
    {
        if (interactionPanel)
            interactionPanel.SetActive(false);
    }

    /// <summary>
    /// Воспроизводит звук загрузки (заглушка)
    /// </summary>
    void PlayLoadSound()
    {
        // Здесь можно добавить AudioSource.PlayOneShot()
    }

    /// <summary>
    /// Воспроизводит звук выгрузки (заглушка)
    /// </summary>
    void PlayUnloadSound()
    {
        // Здесь можно добавить AudioSource.PlayOneShot()
    }

    /// <summary>
    /// Отладочная информация в редакторе
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Показываем зону взаимодействия
        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    #if UNITY_EDITOR
    [ContextMenu("DEBUG/Force Show Panel")]
    void DebugShowPanel()
    {
        ShowInteractionPanel();
        isPlayerNearby = true;
    }

    [ContextMenu("DEBUG/Force Hide Panel")]  
    void DebugHidePanel()
    {
        HideInteractionPanel();
        isPlayerNearby = false;
    }
    #endif
}