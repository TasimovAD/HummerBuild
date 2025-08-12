using UnityEngine;

public class InventoryChangeLogger : MonoBehaviour {
    public InventoryProvider provider;
    public string tagName = "LOGGER";

    void OnEnable(){
        if (provider?.Inventory != null)
            provider.Inventory.OnChanged += Handle;
    }
    void OnDisable(){
        if (provider?.Inventory != null)
            provider.Inventory.OnChanged -= Handle;
    }
    void Handle(){
        int totalStacks = provider.Inventory.stacks.Count;
        Debug.Log($"[{tagName}] OnChanged from {provider.ProviderId}. Stacks: {totalStacks}. Weight={provider.Inventory.CurrentWeightKg:0.0}");
    }
}
