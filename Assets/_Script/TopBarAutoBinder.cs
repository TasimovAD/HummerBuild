using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopBarAutoBinder : MonoBehaviour
{
    [Header("Источники (суммируются)")]
    public List<InventoryProvider> sources = new();

    [Header("Реестр типов (SO)")]
    public ResourceRegistry registry;

    [Header("Валюта")]
    public Wallet wallet;
    public string moneySuffix = "$";

    [Header("Имена дочерних узлов")]
    public string countChildName = "count";
    public string imageChildName = "Image";

    [Header("Опции")]
    public bool writeOnlyNumber = true;
    public bool autoSetIconFromSO = true;
    public bool debugLogs = true;

    // ==== внутреннее ====
    private class Entry {
        public string id;
        public ResourceType type;
        public TMP_Text tmp;
        public Text ugui;
        public Image icon;
        public void SetText(string s){
            if (tmp) tmp.text = s;
            else if (ugui) ugui.text = s;
        }
        public bool HasLabel => tmp || ugui;
    }

    private readonly List<Entry> _entries = new();
    private readonly HashSet<Inventory> _subscribed = new();
    private System.Action _refreshCached;
    private System.Action<long> _walletHandler;

    void Awake(){
        BuildEntries();
    }

    void OnEnable(){
        _refreshCached = RefreshAll;
        TrySubscribeAll();
        RefreshAll();
        InvokeRepeating(nameof(TrySubscribeAll), 0.25f, 0.5f);

        if (wallet != null){
            _walletHandler = _ => RefreshMoney();
            wallet.OnChanged += _walletHandler;
        }
    }

    void OnDisable(){
        CancelInvoke(nameof(TrySubscribeAll));
        foreach (var inv in _subscribed) inv.OnChanged -= _refreshCached;
        _subscribed.Clear();
        if (wallet != null && _walletHandler != null) wallet.OnChanged -= _walletHandler;
        _walletHandler = null;
    }

    void BuildEntries(){
        _entries.Clear();

        foreach (Transform child in transform){
            var key = child.name.Trim().ToLower();
            if (key == "money") continue;

            // текст
            TMP_Text tmp = null;
            Text ugui = null;
            var countTr = child.Find(countChildName);
            if (countTr){
                tmp = countTr.GetComponent<TMP_Text>();
                if (!tmp) ugui = countTr.GetComponent<Text>();
            }
            if (!tmp && !ugui){
                tmp = child.GetComponentInChildren<TMP_Text>(true);
                if (!tmp) ugui = child.GetComponentInChildren<Text>(true);
            }

            // иконка
            Image icon = null;
            var imgTr = child.Find(imageChildName);
            if (imgTr) icon = imgTr.GetComponent<Image>();
            if (!icon) icon = child.GetComponentInChildren<Image>(true);

            // тип из реестра по id==имя узла
            if (!registry){
                if (debugLogs) Debug.LogWarning("[TopBar] Registry not set");
                continue;
            }
            var type = registry.all.FirstOrDefault(t => t && t.id.Trim().ToLower() == key);
            if (!type){
                if (debugLogs) Debug.LogWarning($"[TopBar] Type with id='{key}' not found in registry for node '{child.name}'");
                continue;
            }
            if (!tmp && !ugui){
                if (debugLogs) Debug.LogWarning($"[TopBar] No text component (TMP or Text) found in '{child.name}' (looking for child '{countChildName}')");
                continue;
            }

            var e = new Entry{ id = type.id, type = type, tmp = tmp, ugui = ugui, icon = icon };
            _entries.Add(e);

            if (autoSetIconFromSO && icon && type.icon) icon.sprite = type.icon;

            if (debugLogs) Debug.Log($"[TopBar] Bound '{child.name}' -> '{type.id}', text={(tmp? "TMP":"Text")}");
        }

        RefreshMoney(); // на всякий случай
    }

    void TrySubscribeAll(){
        bool anyNew = false;
        foreach (var src in sources){
            var inv = src ? src.Inventory : null;
            if (inv == null) continue;
            if (_subscribed.Contains(inv)) continue;

            inv.OnChanged += _refreshCached;
            _subscribed.Add(inv);
            anyNew = true;
            if (debugLogs) Debug.Log($"[TopBar] Subscribed to {src.ProviderId}");
        }
        if (anyNew) RefreshAll();
    }

    public void RefreshAll(){
        foreach (var e in _entries){
            int sum = 0;
            foreach (var src in sources){
                var inv = src ? src.Inventory : null;
                if (inv == null) continue;
                sum += inv.GetAmount(e.type);
            }
            e.SetText(writeOnlyNumber ? sum.ToString() : $"{e.type.displayName} {sum}");
            if (debugLogs) Debug.Log($"[TopBar] {e.id} = {sum}");
        }
        RefreshMoney();
    }

    void RefreshMoney(){
        if (!wallet) return;
        var moneyTr = transform.Find("Money");
        if (!moneyTr) return;

        TMP_Text tmp = null;
        Text ugui = null;

        var ct = moneyTr.Find(countChildName);
        if (ct){
            tmp = ct.GetComponent<TMP_Text>();
            if (!tmp) ugui = ct.GetComponent<Text>();
        }
        if (!tmp && !ugui){
            tmp = moneyTr.GetComponentInChildren<TMP_Text>(true);
            if (!tmp) ugui = moneyTr.GetComponentInChildren<Text>(true);
        }
        if (!tmp && !ugui) return;

        var text = wallet.Amount.ToString("#,0").Replace(',', ' ').Replace('\u00A0',' ') + moneySuffix;
        if (tmp) tmp.text = text; else ugui.text = text;
    }
}
