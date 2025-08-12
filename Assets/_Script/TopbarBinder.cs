using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopbarBinder : MonoBehaviour
{
    [Header("Откуда брать ресурсы (суммируются)")]
    public List<InventoryProvider> sources = new();

    [Header("Реестр типов (SO)")]
    public ResourceRegistry registry;

    [Header("Валюта")]
    public Wallet wallet;
    public string moneySuffix = "$";

    [Header("UI-элементы топбара")]
    public ItemUI stone, water, sand, log, cement;
    public MoneyUI money;

    [Header("Отладка")]
    public bool debugLogs = false;

    private readonly List<Inventory> _subs = new();
    private Action<long> _walletHandler;

    [Serializable]
    public class ItemUI {
        public Transform root;
        public ResourceType type;
        public TMP_Text label;
        public Image icon;
    }
    [Serializable]
    public class MoneyUI {
        public Transform root;
        public TMP_Text label;
        public Image icon;
    }

    void Awake(){
        AutoWireItem(stone);
        AutoWireItem(water);
        AutoWireItem(sand);
        AutoWireItem(log);
        AutoWireItem(cement);

        if (money != null){
            if (money.root && !money.label) money.label = money.root.GetComponentInChildren<TMP_Text>(true);
            if (money.root && !money.icon)  money.icon  = money.root.GetComponentInChildren<Image>(true);
        }

        // Если типы не заданы — попробуем сопоставить по имени узлов (Stone/Water/…)
        TryAutoAssignTypesByRootNames();
    }

    void OnEnable(){
        SubscribeInventories();
        RefreshAll();

        if (wallet != null){
            _walletHandler = _ => RefreshMoney();
            wallet.OnChanged += _walletHandler;
        }
    }
    void OnDisable(){
        foreach (var inv in _subs) inv.OnChanged -= RefreshAll;
        _subs.Clear();
        if (wallet != null && _walletHandler != null) wallet.OnChanged -= _walletHandler;
        _walletHandler = null;
    }

    void AutoWireItem(ItemUI it){
        if (it == null || it.root == null) return;
        if (!it.label) it.label = it.root.GetComponentInChildren<TMP_Text>(true);
        if (!it.icon)  it.icon  = it.root.GetComponentInChildren<Image>(true);
    }

    void SubscribeInventories(){
        foreach (var s in sources){
            if (s?.Inventory == null) continue;
            s.Inventory.OnChanged += RefreshAll;
            _subs.Add(s.Inventory);
            if (debugLogs) Debug.Log($"[Topbar] Subscribed to: {s.ProviderId}");
        }
    }

    public void RefreshAll(){
        RefreshItem(stone);
        RefreshItem(water);
        RefreshItem(sand);
        RefreshItem(log);
        RefreshItem(cement);
        RefreshMoney();
    }

    void RefreshItem(ItemUI it){
        if (it == null || it.root == null || it.label == null) return;
        if (it.type == null){
            it.label.text = "—";
            return;
        }

        int sum = 0;
        foreach (var s in sources)
            if (s?.Inventory != null)
                sum += s.Inventory.GetAmount(it.type);

        it.label.text = $"{it.type.displayName} {sum}";
        if (it.icon && it.type.icon) it.icon.sprite = it.type.icon;

        if (debugLogs) Debug.Log($"[Topbar] {it.type.id} = {sum}");
    }

    void RefreshMoney(){
        if (money == null || money.root == null || money.label == null || wallet == null) return;
        money.label.text = $"{FormatMoney(wallet.Amount)}{moneySuffix}";
    }

    string FormatMoney(long v) => v.ToString("#,0").Replace(',', ' ').Replace('\u00A0', ' ');

    [ContextMenu("Auto Assign Types From Registry By Names")]
    void TryAutoAssignTypesByRootNames(){
        if (!registry) return;
        MapByRootName(stone);
        MapByRootName(water);
        MapByRootName(sand);
        MapByRootName(log);
        MapByRootName(cement);
    }

    void MapByRootName(ItemUI it){
        if (it == null || it.type != null || it.root == null) return;
        string key = it.root.name.Trim().ToLower(); // "Stone" → "stone"
        var t = registry.all.FirstOrDefault(x => x && x.id.Trim().ToLower() == key);
        if (t) {
            it.type = t;
            if (debugLogs) Debug.Log($"[Topbar] Auto-mapped {it.root.name} → {t.id}");
        }
    }
}
