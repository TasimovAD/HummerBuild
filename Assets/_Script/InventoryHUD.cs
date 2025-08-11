using System.Text;
using UnityEngine;
using TMPro;

public class InventoryHUD : MonoBehaviour {
    public InventoryProvider provider; // CharacterInventory
    public TMP_Text text;

    private Inventory _subscribed; // на какой инвентарь подписались

    void OnEnable() {
        TrySubscribe();
        Refresh();
    }

    void OnDisable() {
        TryUnsubscribe();
    }

    void Update() {
        // На случай, если провайдер/инвентарь появился позже
        if (_subscribed == null) TrySubscribe();
    }

    private void TrySubscribe(){
        if (!provider) return;
        if (provider.Inventory == null) return;

        if (_subscribed == provider.Inventory) return;

        TryUnsubscribe();
        _subscribed = provider.Inventory;
        _subscribed.OnChanged += Refresh;
        Debug.Log($"[InventoryHUD] Subscribed to {provider.ProviderId}");
    }

    private void TryUnsubscribe(){
        if (_subscribed != null){
            _subscribed.OnChanged -= Refresh;
            _subscribed = null;
        }
    }

    public void Refresh(){
        if (!provider || provider.Inventory == null || !text) return;

        var inv = provider.Inventory;
        var sb = new StringBuilder();
        sb.AppendLine(provider.ProviderId);
        foreach (var st in inv.stacks)
            if (st.type) sb.AppendLine($"{st.type.displayName}: {st.amount}");

        text.text = sb.ToString();
        // Для отладки:
        Debug.Log($"[InventoryHUD] Refresh for {provider.ProviderId}");
    }
}