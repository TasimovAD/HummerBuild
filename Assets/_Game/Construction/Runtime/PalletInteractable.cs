using UnityEngine;

/// Навесь на GO палеты. Это "точка взаимодействия" игрока.
[RequireComponent(typeof(Collider))]
public class PalletInteractable : MonoBehaviour
{
    [Header("Что лежит на палете")]
    public ResourceDef resource;

    [Header("Чей инвентарь отображает палета")]
    public InventoryProviderAdapter inventory; // например: Warehouse или BuildSite.Buffer

    [Header("Необязательно, чисто для навигации/отладок")]
    public ResourcePalletSlots slots; // можно не задавать

    /// Есть ли хотя бы 1 ед. ресурса в этом инвентаре?
    public bool HasAny()
    {
        if (!inventory || !resource) return false;
        return inventory.Get(resource) > 0;
    }

    /// Игрок забирает 1 ед. (из инвентаря), визуал сам обновится через PalletGroupManager.OnChanged
    public bool TryTakeOne(out ResourceDef res)
    {
        res = null;
        if (!inventory || !resource) return false;

        int removed = inventory.Remove(resource, 1);
        if (removed > 0)
        {
            res = resource;
            return true;
        }
        return false;
    }

    /// Игрок кладёт 1 ед. в этот инвентарь
    public bool TryPutOne(ResourceDef res)
    {
        if (!inventory || !res) return false;
        int added = inventory.Add(res, 1);
        return added > 0;
    }
}
