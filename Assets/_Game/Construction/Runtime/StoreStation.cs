using UnityEngine;

public class StoreStation : MonoBehaviour
{
    [Header("Refs")]
    public StoreCatalog Catalog;
    public PalletGroupManager StorePallets;       // палеты перед магазином
    public InventoryProviderAdapter StoreInventory; // счётчик ресурсов магазина (виртуальный склад)

    // Покупка N единиц, списываем деньги у игрока и пополняем палеты магазина
    public bool TryBuy(ResourceDef res, int units, WalletPlayer buyerWallet)
    {
        if (units <= 0 || res == null || buyerWallet == null) return false;

        var price = Catalog.Get(res);
        if (price == null) return false;

        int totalCost = units * Mathf.Max(1, price.PricePerUnit);
        if (!buyerWallet.TrySpend(totalCost)) return false;

        // Увеличиваем кол-во в инвентаре магазина…
        StoreInventory.Add(res, units);                              // 
        // …и пересобираем визуал палет под это количество.
        StorePallets.RebuildAll();                                   // 
        return true;
    }
}
