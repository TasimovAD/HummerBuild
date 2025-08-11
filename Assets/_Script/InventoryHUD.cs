using System.Text;
using UnityEngine;
using TMPro;

public class InventoryHUD : MonoBehaviour {
    public InventoryProvider provider;
    public TMP_Text text;

    void OnEnable(){ if (provider) provider.Inventory.OnChanged += Refresh; Refresh(); }
    void OnDisable(){ if (provider) provider.Inventory.OnChanged -= Refresh; }

    public void Refresh(){
        if (!provider || !text) return;
        var sb = new StringBuilder();
        sb.AppendLine(provider.ProviderId);
        foreach (var st in provider.Inventory.stacks)
            if (st.type) sb.AppendLine($"{st.type.displayName}: {st.amount}");
        text.text = sb.ToString();
    }
}
