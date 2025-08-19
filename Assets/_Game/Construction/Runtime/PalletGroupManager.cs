using UnityEngine;
using System.Collections.Generic;

public class PalletGroupManager : MonoBehaviour
{
    public InventoryProviderAdapter inventory;
    public List<ResourcePallet> pallets;

    void Start()
    {
        if (inventory != null)
            inventory.OnChanged += RebuildAll;

        RebuildAll();
    }

    void RebuildAll(ResourceDef _) => RebuildAll();

    void RebuildAll()
    {
        foreach (var pallet in pallets)
        {
            if (!pallet || pallet.resource == null) continue;
            int count = inventory.Get(pallet.resource);
            var prefab = pallet.resource.CarryProp;
            pallet.Rebuild(count, prefab);
        }
    }

    public GameObject Take(ResourceDef res)
    {
        var pallet = pallets.Find(p => p.resource == res);
        if (!pallet) return null;

        inventory.Remove(res, 1); // снимаем 1 из инвентаря
        return pallet.TakeOne();
    }
}
