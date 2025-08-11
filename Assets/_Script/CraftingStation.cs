using System.Collections;
using UnityEngine;

public class CraftingStation : MonoBehaviour {
    public StorageInventory storage; // где берём и куда кладём
    public ResourceType outType;     // mix
    public ResourceNeed[] inputs;    // cement,sand,water
    public int outAmount = 10;
    public float craftTime = 3f;
    public UnityEngine.UI.Slider progressBar;

    private bool _busy;

    public bool CanCraft(){
        if (_busy) return false;
        foreach (var n in inputs)
            if (storage.Inventory.GetAmount(n.type) < n.amount) return false;
        return true;
    }

    public void StartCraft(){
        if (!CanCraft()) return;
        foreach (var n in inputs) storage.Inventory.Remove(n.type, n.amount);
        StartCoroutine(Craft());
    }

    IEnumerator Craft(){
        _busy = true;
        float t=0;
        while (t<craftTime){ t+=Time.deltaTime; if (progressBar) progressBar.value=t/craftTime; yield return null; }
        _busy = false;
        if (progressBar) progressBar.value=0;
        storage.Inventory.Add(outType, outAmount);
    }
}
