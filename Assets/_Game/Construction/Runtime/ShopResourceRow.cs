// Assets/_Game/Store/ShopResourceRow.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopResourceRow : MonoBehaviour
{
    [Header("Bind")]
    public ResourceDef resource;         // что продаём
    public int unitPrice = 10;           // цена за 1 шт
    public int minCount = 0;
    public int maxCount = 999;

    [Header("UI")]
    public TMP_Text nameText;
    public Image iconImage;
    public TMP_Text countText;
    public Button minusBtn;
    public Button plusBtn;

    public int Count { get; private set; }

    public int Subtotal => unitPrice * Count;   // локальная сумма за ресурс

    public System.Action<ShopResourceRow> onChanged; // сообщаем панели об изменениях

    void Awake()
    {
        if (minusBtn) minusBtn.onClick.AddListener(() => SetCount(Count - 1));
        if (plusBtn)  plusBtn.onClick.AddListener(() => SetCount(Count + 1));
        RefreshStatic();
        SetCount(0, silent:true);
    }

    public void Bind(ResourceDef def, int price)
    {
        resource = def;
        unitPrice = price;
        RefreshStatic();
        SetCount(Count, silent:true);
    }

    void RefreshStatic()
    {
        if (nameText && resource) nameText.text = resource.DisplayName;
        if (iconImage) iconImage.sprite = resource ? resource.Icon : null;
    }

    public void SetCount(int value, bool silent = false)
    {
        Count = Mathf.Clamp(value, minCount, maxCount);
        if (countText) countText.text = Count.ToString();
        if (!silent) onChanged?.Invoke(this);
    }

    public void ResetToZero()
    {
        SetCount(0);
    }
}
