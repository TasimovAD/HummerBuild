using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Отдельный слот в мини-инвентаре
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;                    // иконка ресурса
    public Image backgroundImage;              // фон слота (опционально)
    public TMP_Text numberText;                // номер слота (1, 2, 3...)
    public Button slotButton;                  // кнопка для клика

    [Header("Visual States")]
    public Sprite emptySlotSprite;             // спрайт пустого слота
    public Color normalColor = Color.white;
    public Color emptyColor = new Color(1,1,1,0.5f);
    public Color highlightColor = new Color(1,1,0,1); // для выделения выбранного слота

    private int slotIndex;
    private MobileInventoryUI parentInventory;
    private ResourceDef currentResource;
    private bool isEmpty = true;

    /// <summary>
    /// Инициализация слота
    /// </summary>
    public void Initialize(int index, MobileInventoryUI parent)
    {
        slotIndex = index;
        parentInventory = parent;
        
        // Устанавливаем номер слота
        if (numberText != null)
        {
            numberText.text = index.ToString();
        }

        // Настраиваем кнопку
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        SetEmpty();
    }

    /// <summary>
    /// Установить ресурс в слот
    /// </summary>
    public void SetResource(ResourceDef resource)
    {
        if (resource == null)
        {
            SetEmpty();
            return;
        }

        currentResource = resource;
        isEmpty = false;

        // Устанавливаем иконку
        if (iconImage != null)
        {
            if (resource.Icon != null)
            {
                iconImage.sprite = resource.Icon;
            }
            else
            {
                iconImage.sprite = emptySlotSprite;
            }
            iconImage.color = normalColor;
        }

        // Фон слота
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    /// <summary>
    /// Сделать слот пустым
    /// </summary>
    public void SetEmpty()
    {
        currentResource = null;
        isEmpty = true;

        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.color = emptyColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
        }
    }

    /// <summary>
    /// Выделить слот (например, при выборе инструмента)
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (backgroundImage != null)
        {
            if (highlighted && !isEmpty)
            {
                backgroundImage.color = highlightColor;
            }
            else
            {
                backgroundImage.color = isEmpty ? emptyColor : normalColor;
            }
        }
    }

    /// <summary>
    /// Обработчик клика по слоту
    /// </summary>
    public void OnSlotClicked()
    {
        if (parentInventory != null)
        {
            parentInventory.OnSlotClicked(slotIndex - 1); // -1 потому что индексы с 0
        }
    }

    /// <summary>
    /// Получить текущий ресурс в слоте
    /// </summary>
    public ResourceDef GetCurrentResource()
    {
        return currentResource;
    }

    /// <summary>
    /// Проверить, пустой ли слот
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }
}