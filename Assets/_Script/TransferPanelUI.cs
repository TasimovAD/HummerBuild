using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransferPanelUI : MonoBehaviour {
    [Header("UI")]
    public TMP_Dropdown typeDropdown;
    public TMP_InputField amountInput;
    public Button btnTransfer;         // Перенести -> from -> to
    public Button btnTransferAll;      // Перенести всё выбранного типа
    public Button btnSwap;             // Поменять направления местами
    public TMP_Text fromInfo;
    public TMP_Text toInfo;

    [Header("Данные")]
    public ResourceRegistry registry;

    private InventoryProvider _from;
    private InventoryProvider _to;
    private List<ResourceType> _types = new();

    public void Bind(InventoryProvider from, InventoryProvider to){
        _from = from; _to = to;
        RebuildTypes();
        RefreshInfo();
        HookButtons();
    }

    void OnEnable(){
        if (_from?.Inventory != null) _from.Inventory.OnChanged += RefreshInfo;
        if (_to?.Inventory   != null) _to.Inventory.OnChanged   += RefreshInfo;
    }
    void OnDisable(){
        if (_from?.Inventory != null) _from.Inventory.OnChanged -= RefreshInfo;
        if (_to?.Inventory   != null) _to.Inventory.OnChanged   -= RefreshInfo;
    }

    void RebuildTypes(){
        _types.Clear();
        var opts = new List<TMP_Dropdown.OptionData>();
        foreach (var st in _from.Inventory.stacks){
            if (st.type == null) continue;
            if (_types.Contains(st.type)) continue;
            _types.Add(st.type);
        }
        if (_types.Count == 0 && registry != null){
            // fallback — возьмём из реестра, чтобы дропдаун не пустел
            foreach (var t in registry.all) _types.Add(t);
        }
        typeDropdown.ClearOptions();
        foreach (var t in _types) opts.Add(new TMP_Dropdown.OptionData(t.displayName));
        typeDropdown.AddOptions(opts);
        typeDropdown.value = 0;
    }

    void HookButtons(){
        btnTransfer.onClick.RemoveAllListeners();
        btnTransferAll.onClick.RemoveAllListeners();
        btnSwap.onClick.RemoveAllListeners();

        btnTransfer.onClick.AddListener(() => {
            var type = GetSelectedType();
            int.TryParse(amountInput.text, out var amt);
            amt = Mathf.Max(1, amt);
            var moved = Inventory.Transfer(_from.Inventory, _to.Inventory, type, amt);
            Debug.Log($"[TransferUI] Moved {moved} x {type.id}");
            RefreshInfo();
        });

        btnTransferAll.onClick.AddListener(() => {
            var type = GetSelectedType();
            int have = _from.Inventory.GetAmount(type);
            var moved = Inventory.Transfer(_from.Inventory, _to.Inventory, type, have);
            Debug.Log($"[TransferUI] Moved ALL {moved} x {type.id}");
            RefreshInfo();
        });

        btnSwap.onClick.AddListener(() => {
            (_from, _to) = (_to, _from);
            RebuildTypes();
            RefreshInfo();
        });
    }

    ResourceType GetSelectedType(){
        if (_types.Count == 0) return null;
        int idx = Mathf.Clamp(typeDropdown.value, 0, _types.Count-1);
        return _types[idx];
    }

    public void RefreshInfo(){
        if (fromInfo)
            fromInfo.text = BuildInfo(_from);
        if (toInfo)
            toInfo.text = BuildInfo(_to);
    }

    string BuildInfo(InventoryProvider p){
        if (p == null) return "-";
        var inv = p.Inventory;
        System.Text.StringBuilder sb = new();
        sb.AppendLine(p.ProviderId);
        foreach (var s in inv.stacks)
            if (s.type) sb.AppendLine($"{s.type.displayName}: {s.amount}");
        return sb.ToString();
    }
}
