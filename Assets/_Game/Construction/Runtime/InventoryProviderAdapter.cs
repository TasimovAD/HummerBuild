// Assets/_HummerBuild/Construction/Runtime/InventoryProviderAdapter.cs
using UnityEngine;
using System;

public class InventoryProviderAdapter : MonoBehaviour, IInventory
{
    [Tooltip("Ссылка на твой существующий InventoryProvider (Storage/Vehicle/Character/etc.)")]
    public MonoBehaviour Provider; // сюда в инспекторе подкидываешь твой StorageInventory (наследник InventoryProvider)

    // ====== ВАЖНО ======
    // Ниже вызовы нужно связать с твоими методами InventoryProvider: Get/ Add/ Remove/ OnChanged.
    // Переименуй под свой API. Я оставляю “шаблон” вызовов и комментарии:

    public event Action<ResourceDef> OnChanged;

    // Пример: если у тебя InventoryProvider работает с ResourceDef напрямую:
    public int Get(ResourceDef r)
    {
        // return ((InventoryProvider)Provider).Get(r);
        return SafeInvoke<int>("Get", r);
    }

    public int Add(ResourceDef r, int amount)
    {
        // var added = ((InventoryProvider)Provider).Add(r, amount);
        // if (added > 0) OnChanged?.Invoke(r);
        // return added;
        var added = SafeInvoke<int>("Add", r, amount);
        if (added > 0) OnChanged?.Invoke(r);
        return added;
    }

    public int Remove(ResourceDef r, int amount)
    {
        // var removed = ((InventoryProvider)Provider).Remove(r, amount);
        // if (removed > 0) OnChanged?.Invoke(r);
        // return removed;
        var removed = SafeInvoke<int>("Remove", r, amount);
        if (removed > 0) OnChanged?.Invoke(r);
        return removed;
    }

    // Подписка на твой Inventory.OnChanged:
    void OnEnable()
    {
        // ((InventoryProvider)Provider).OnChanged += HandleProviderChanged;
        SafeHook("OnChanged", true);
    }
    void OnDisable()
    {
        SafeHook("OnChanged", false);
    }

    void HandleProviderChanged(ResourceDef r, int delta)
    {
        OnChanged?.Invoke(r);
    }

    // ===== Универсальные хелперы через рефлексию (чтобы не ломалось, если методы названы иначе) =====
    T SafeInvoke<T>(string method, params object[] args)
    {
        if (Provider == null) return default;
        var m = Provider.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (m == null) { Debug.LogWarning($"Method {method} not found on {Provider.GetType().Name}"); return default; }
        var res = m.Invoke(Provider, args);
        return res is T t ? t : default;
    }

    void SafeHook(string evtName, bool subscribe)
    {
        if (Provider == null) return;
        var e = Provider.GetType().GetEvent(evtName);
        if (e == null) return;
        var handler = (Action<ResourceDef, int>)HandleProviderChanged;
        if (subscribe) e.AddEventHandler(Provider, handler);
        else e.RemoveEventHandler(Provider, handler);
    }
}
