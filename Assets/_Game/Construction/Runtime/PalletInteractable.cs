using UnityEngine;

/// Вешай на объект-палету, где уже есть компонент ResourcePalletSlots.
/// Даёт простой API для взятия/возврата готовых пропов (из слотов).
[RequireComponent(typeof(ResourcePalletSlots))]
public class PalletInteractable : MonoBehaviour
{
    public ResourcePalletSlots slots;

    void Reset()
    {
        slots = GetComponent<ResourcePalletSlots>();
    }

    public bool TryTakeOne(out GameObject prop)
    {
        prop = null;
        if (!slots) return false;

        var go = slots.Take();
        if (!go) return false;

        prop = go;
        return true;
    }

    public bool TryPutOne(GameObject prop)
    {
        if (!slots || !prop) return false;
        // кладём по слоту: без ребилда — просто поставить
        return slots.TryAdd(prop);
    }
}
