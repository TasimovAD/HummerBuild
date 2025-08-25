// Assets/_Game/Construction/Runtime/PalletInteractable.cs
using UnityEngine;

/// Вешай на объект-палету (рядом должен быть ResourcePalletSlots).
/// Умеет: взять/положить проп И синхронизировать это с инвентарём через InventoryProviderAdapter.
[RequireComponent(typeof(ResourcePalletSlots))]
public class PalletInteractable : MonoBehaviour
{
    public enum PalletKind
    {
        VisualOnly,     // только визуал (не пишет в инвентарь)
        Storage,        // связан со складским InventoryProviderAdapter (склад базы)
        BuildBuffer,     // связан с буфером стройплощадки (BuildSite.Buffer)
        Shop
    }

    [Header("Связи")]
    public ResourcePalletSlots slots;

    [Tooltip("Тип палеты: влияет на то, в какой инвентарь писать")]
    public PalletKind kind = PalletKind.VisualOnly;

    [Tooltip("К какому инвентарю привязать (Storage или Buffer)")]
    public InventoryProviderAdapter linkedInventory;

    void Reset()
    {
        slots = GetComponent<ResourcePalletSlots>();
    }

    /// Забрать 1 объект из палеты (если получится). Возвращает GameObject.
    /// ВАЖНО: если палета привязана к инвентарю — тут же делаем Remove(… ,1)
    public bool TryTakeOne(out GameObject prop)
    {
        prop = null;
        if (!slots) return false;

        // 1) Если палета привязана к инвентарю, проверим, есть ли ресурс в нём
        if (linkedInventory && slots.Resource)
        {
            int have = linkedInventory.Get(slots.Resource);
            if (have <= 0)
            {
                // В инвентаре пусто — не даём взять, чтобы не разъезжался баланс
                return false;
            }
        }

        // 2) Визуально взять из слотов
        var go = slots.Take();
        if (!go)
        {
            // На всякий случай: если визуально пусто — тоже не даём
            return false;
        }

        // 3) Синхронизировать с инвентарём
        if (linkedInventory && slots.Resource)
        {
            // Remove 1 ед. из привязанного инвентаря
            int removed = linkedInventory.Remove(slots.Resource, 1);
            if (removed <= 0)
            {
                // не вышло — вернём проп обратно в слот
                slots.TryAdd(go);
                return false;
            }
        }

        prop = go;
        return true;
    }

    /// Положить 1 объект на палету. Если привязана к инвентарю — Add(… ,1).
    /// Если не поместился в слот — вернём false.
    public bool TryPutOne(GameObject prop)
    {
        if (!prop || !slots) return false;

        // 1) визуально попытаться положить в свободный слот
        bool ok = slots.TryAdd(prop);
        if (!ok) return false;

        // 2) синхронизировать с инвентарём
        if (linkedInventory && slots.Resource)
        {
            int added = linkedInventory.Add(slots.Resource, 1);
            if (added <= 0)
            {
                // не добавилось — откат: забрать из слота обратно наружу
                var takenBack = slots.Take();
                if (takenBack)
                {
                    takenBack.transform.SetParent(null);
                }
                return false;
            }
        }

        return true;
    }
}
