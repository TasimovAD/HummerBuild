// Assets/_Game/Store/ShopPanelSimple.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopPanelSimple : MonoBehaviour
{
    [Header("Refs")]
    public Wallet playerWallet;               // кошелёк игрока
    public StorageInventory storeStorage;     // склад магазина (куда падают покупки)
    public InventoryProviderAdapter storeAdapter; // опц. если используешь адаптер поверх Storage

    [Header("Rows (5 фиксированных)")]
    public ShopResourceRow rowWood;
    public ShopResourceRow rowSand;
    public ShopResourceRow rowConcrete;
    public ShopResourceRow rowStone;
    public ShopResourceRow rowWater;

    [Header("Total & Buy")]
    public TMP_Text totalText;                // "ИТОГО: 2500$"
    public Button buyButton;
    public string moneySuffix = "$";

    // внутреннее
    readonly List<ShopResourceRow> _rows = new();

    void Awake()
    {
        // Подпишем все строки
        TryAddRow(rowWood);
        TryAddRow(rowSand);
        TryAddRow(rowConcrete);
        TryAddRow(rowStone);
        TryAddRow(rowWater);

        foreach (var r in _rows) r.onChanged += _ => RecalcTotal();
        if (buyButton) buyButton.onClick.AddListener(OnBuyClicked);

        RecalcTotal();
    }

    void TryAddRow(ShopResourceRow r)
    {
        if (!r) return;
        _rows.Add(r);
    }

    void RecalcTotal()
    {
        long total = 0;
        foreach (var r in _rows) total += r.Subtotal;

        if (totalText) totalText.text = $"{total}{moneySuffix}";

        // выключаем кнопку, если кошелёк пуст или нечего покупать
        bool hasAny = false;
        foreach (var r in _rows) if (r.Count > 0) { hasAny = true; break; }

        bool canBuy = hasAny && playerWallet != null && playerWallet.CanSpend(total); // :contentReference[oaicite:3]{index=3}
        if (buyButton) buyButton.interactable = canBuy;
    }

    void OnBuyClicked()
    {
        long total = 0;
        foreach (var r in _rows) total += r.Subtotal;

        if (playerWallet == null) return;
        if (!playerWallet.CanSpend(total)) { RecalcTotal(); return; } // защита от гонок  :contentReference[oaicite:4]{index=4}

        // списываем деньги
        playerWallet.Spend(total); // :contentReference[oaicite:5]{index=5}

        // зачисляем товар на склад магазина
        foreach (var r in _rows)
        {
            if (r.resource == null || r.Count <= 0) continue;

            // Вариант 1: через адаптер (если он уже маппит на StorageInventory)
            if (storeAdapter)
            {
                storeAdapter.Add(r.resource, r.Count);
            }
            // Вариант 2: напрямую в StorageInventory
            else if (storeStorage)
            {
                storeStorage.AddItem(r.resource, r.Count); // ScriptableObject + amount  :contentReference[oaicite:6]{index=6}
            }

            // Обнуляем UI-количество после покупки
            r.ResetToZero();
        }

        RecalcTotal();
        // дальше — спавн визуала на палетах магазина, если он завязан на Storage/PalletGroupManager
        // (обычно достаточно, чтобы PalletGroupManager смотрел на тот же InventoryProviderAdapter/Storage)
    }
}
