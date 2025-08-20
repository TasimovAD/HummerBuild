using UnityEngine;
using System.Collections.Generic;

public class PalletGroupManager : MonoBehaviour
{
    [System.Serializable]
    public class ResourcePalletBinding
    {
        public ResourceDef resource;
        public ResourcePalletSlots palletSlots;
    }

    public List<ResourcePalletBinding> bindings = new();

    public ResourcePalletSlots GetPalletFor(ResourceDef res)
    {
        if (!res) return null;
        foreach (var b in bindings)
        {
            if (b.resource == res)
                return b.palletSlots;
        }
        return null;
    }

    public bool TryAddVisual(ResourceDef res, GameObject prefab)
    {
        var pallet = GetPalletFor(res);
        if (pallet == null) return false;

        return pallet.TryAdd(prefab);
    }

    public GameObject Take(ResourceDef res)
    {
        var pallet = GetPalletFor(res);
        if (pallet == null) return null;

        return pallet.Take();
    }

    public void ClearAll()
    {
        foreach (var b in bindings)
        {
            if (b.palletSlots != null)
                b.palletSlots.ClearAll();
        }
    }
}

