using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopBarAutoBinder : MonoBehaviour
{
    [Header("Откуда суммировать ресурсы")]
    public List<InventoryProvider> sources = new();   // напр., только StorageInventory (MainStorage)

    [Header("Реестр типов (SO)")]
    public ResourceRegistry registry;

    [Header("Валюта (Money)")]
    public Wallet wallet;              // опционально
    public string moneySuffix = "$";

    [Header("Имена дочерних узлов")]
    public string countChildName = "count";   // в каждом блоке (Log/Stone/...) должен быть TMP Text с этим именем
    public string imageChildName = "Image";   // иконка (необязательно)

    [Header("Опции")]
    public bool writeOnlyNumber = true;       // true: в count только число (рекомендуется); false: "Лес 25"
    public bool autoSetIconFromSO = true;     // если в SO есть иконка, подставим в Image
    public bool debugLogs = false;

    // ===== внутреннее =====
    private class Entry {
        public string id;              // "log", "stone", ...
        public ResourceType type;      // ссылка на SO
        public TMP_Text label;         // текст count
        public Image icon;             // иконка (может быть null)
    }

    private readonly List<Entry> _entries = new();
    private readonly List<Inventory> _subs = new();
    private System.Action<long> _walletHandler;

    void Awake()
    {
        BuildEntries();
    }

    void OnEnable()
    {
        SubscribeInventories();
        RefreshAll();

        if (wallet != null) {
            _walletHandler = _ => RefreshMoney();
            wallet.OnChanged += _walletHandler;
        }
    }

    void OnDisable()
    {
        foreach (var inv in _subs) inv.OnChanged -= RefreshAll;
        _subs.Clear();

        if (wallet != null && _walletHandler != null) wallet.OnChanged -= _walletHandler;
        _walletHandler = null;
    }

    // Строим список элементов по детям TopBar
    void BuildEntries()
    {
        _entries.Clear();

        foreach (Transform child in transform)
        {
            var nameLower = child.name.Trim().ToLower();

            if (nameLower == "money") continue; // деньги отдельно

            // ищем count и Image
            TMP_Text label = null;
            Image icon = null;

            var countTr = child.Find(countChildName);
            if (countTr) label = countTr.GetComponent<TMP_Text>();
            if (!label) label = child.GetComponentInChildren<TMP_Text>(true);

            var imgTr = child.Find(imageChildName);
            if (imgTr) icon = imgTr.GetComponent<Image>();
            if (!icon) icon = child.GetComponentInChildren<Image>(true);

            // мапим к ResourceType по id = имени узла
            ResourceType type = null;
            if (registry)
                type = registry.all.FirstOrDefault(t => t && t.id.Trim().ToLower() == nameLower);

            if (type == null)
            {
                if (debugLogs) Debug.LogWarning($"[TopBar] Для узла '{child.name}' не найден ResourceType в Registry (id должен быть '{nameLower}').");
                continue;
            }
            if (!label)
            {
                if (debugLogs) Debug.LogWarning($"[TopBar] У узла '{child.name}' не найден TMP_Text '{countChildName}'.");
                continue;
            }

            var e = new Entry { id = type.id, type = type, label = label, icon = icon };
            _entries.Add(e);

            if (autoSetIconFromSO && icon && type.icon) icon.sprite = type.icon;

            if (debugLogs) Debug.Log($"[TopBar] Привязал '{child.name}' → type='{type.id}'");
        }

        // Деньги
        var moneyTr = transform.Find("Money");
        if (moneyTr && wallet != null)
        {
            var moneyLabel = moneyTr.Find(countChildName)?.GetComponent<TMP_Text>();
            if (!moneyLabel) moneyLabel = moneyTr.GetComponentInChildren<TMP_Text>(true);

            // сразу обновим
            if (moneyLabel) moneyLabel.text = FormatMoney(wallet.Amount) + moneySuffix;
        }
    }

    void SubscribeInventories()
    {
        foreach (var s in sources)
        {
            if (s?.Inventory == null) continue;
            s.Inventory.OnChanged += RefreshAll;
            _subs.Add(s.Inventory);
            if (debugLogs) Debug.Log($"[TopBar] Подписан на {s.ProviderId}");
        }
    }

    public void RefreshAll()
    {
        // ресурсы
        foreach (var e in _entries)
        {
            int sum = 0;
            foreach (var s in sources)
                if (s?.Inventory != null)
                    sum += s.Inventory.GetAmount(e.type);

            if (writeOnlyNumber) e.label.text = sum.ToString();
            else e.label.text = $"{e.type.displayName} {sum}";

            if (debugLogs) Debug.Log($"[TopBar] {e.id} = {sum}");
        }

        // деньги
        RefreshMoney();
    }

    void RefreshMoney()
    {
        if (!wallet) return;
        var moneyTr = transform.Find("Money");
        if (!moneyTr) return;

        var label = moneyTr.Find(countChildName)?.GetComponent<TMP_Text>();
        if (!label) label = moneyTr.GetComponentInChildren<TMP_Text>(true);
        if (!label) return;

        label.text = FormatMoney(wallet.Amount) + moneySuffix;
    }

    string FormatMoney(long v) => v.ToString("#,0").Replace(',', ' ').Replace('\u00A0', ' ');
}
