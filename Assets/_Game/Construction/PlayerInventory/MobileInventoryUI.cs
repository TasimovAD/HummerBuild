using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Мини-инвентарь для мобильной версии игры
/// Показывает 10 слотов с ресурсами и инструментами, которые игрок носит
/// </summary>
public class MobileInventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerCarryController playerCarry;  // контроллер переноски игрока
    public Transform slotsParent;              // родитель всех слотов
    
    [Header("Слоты (присваивать вручную)")]
    public InventorySlotUI slot1;
    public InventorySlotUI slot2;
    public InventorySlotUI slot3;
    public InventorySlotUI slot4;
    public InventorySlotUI slot5;
    public InventorySlotUI slot6;
    public InventorySlotUI slot7;
    public InventorySlotUI slot8;
    public InventorySlotUI slot9;
    public InventorySlotUI slot10;

    [Header("Настройки")]
    public Sprite emptySlotSprite;             // иконка пустого слота
    public Color normalColor = Color.white;
    public Color emptyColor = new Color(1,1,1,0.3f);

    // Внутренний список для удобства работы
    private List<InventorySlotUI> allSlots = new List<InventorySlotUI>();
    private List<ResourceDef> heldResources = new List<ResourceDef>(); // ресурсы в "инвентаре" игрока

    void Awake()
    {
        // Собираем все слоты в список
        allSlots.Add(slot1);
        allSlots.Add(slot2);
        allSlots.Add(slot3);
        allSlots.Add(slot4);
        allSlots.Add(slot5);
        allSlots.Add(slot6);
        allSlots.Add(slot7);
        allSlots.Add(slot8);
        allSlots.Add(slot9);
        allSlots.Add(slot10);
        
        // Удаляем null элементы
        allSlots.RemoveAll(slot => slot == null);
        
        // Инициализация слотов
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i] != null)
            {
                allSlots[i].Initialize(i + 1, this);
            }
        }
    }

    void Start()
    {
        RefreshUI();
    }

    void Update()
    {
        // Проверяем изменения в системе переноски
        CheckForChanges();
    }

    /// <summary>
    /// Проверить изменения в том, что несет игрок
    /// </summary>
    void CheckForChanges()
    {
        if (playerCarry == null) return;

        // Если игрок что-то взял в руки
        if (playerCarry.IsCarrying && playerCarry.CurrentProp != null)
        {
            var carryProp = playerCarry.CurrentProp.GetComponent<CarryPropTag>();
            if (carryProp != null && carryProp.resource != null)
            {
                AddResourceToInventory(carryProp.resource);
            }
        }
        else
        {
            // Если игрок ничего не несет, но в инвентаре что-то есть - значит он что-то положил
            if (heldResources.Count > 0)
            {
                RemoveLastResource();
            }
        }
    }

    /// <summary>
    /// Добавить ресурс в мини-инвентарь
    /// </summary>
    public void AddResourceToInventory(ResourceDef resource)
    {
        if (resource == null) return;

        // Проверяем, не добавляли ли мы уже этот ресурс
        if (heldResources.Contains(resource)) return;

        // Добавляем в первый свободный слот
        if (heldResources.Count < allSlots.Count)
        {
            heldResources.Add(resource);
            RefreshUI();
        }
    }

    /// <summary>
    /// Удалить последний ресурс (когда игрок что-то положил)
    /// </summary>
    public void RemoveLastResource()
    {
        if (heldResources.Count > 0)
        {
            heldResources.RemoveAt(heldResources.Count - 1);
            RefreshUI();
        }
    }

    /// <summary>
    /// Очистить весь инвентарь
    /// </summary>
    public void ClearInventory()
    {
        heldResources.Clear();
        RefreshUI();
    }

    /// <summary>
    /// Обновить отображение UI
    /// </summary>
    public void RefreshUI()
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i] != null)
            {
                if (i < heldResources.Count && heldResources[i] != null)
                {
                    // Слот заполнен
                    allSlots[i].SetResource(heldResources[i]);
                }
                else
                {
                    // Пустой слот
                    allSlots[i].SetEmpty();
                }
            }
        }
    }

    /// <summary>
    /// Клик по слоту (для будущего функционала переключения инструментов)
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        Debug.Log($"Clicked slot {slotIndex + 1}");
        
        // Здесь можно добавить логику переключения между инструментами
        if (slotIndex < heldResources.Count && heldResources[slotIndex] != null)
        {
            Debug.Log($"Selected resource: {heldResources[slotIndex].DisplayName}");
        }
    }

    /// <summary>
    /// Получить ресурс по индексу слота
    /// </summary>
    public ResourceDef GetResourceInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < heldResources.Count)
        {
            return heldResources[slotIndex];
        }
        return null;
    }

    #if UNITY_EDITOR
    [ContextMenu("Test Add Cement")]
    void TestAddCement()
    {
        var cement = Resources.Load<ResourceDef>("cement");
        if (cement != null) AddResourceToInventory(cement);
    }

    [ContextMenu("Test Clear")]
    void TestClear()
    {
        ClearInventory();
    }
    #endif
}