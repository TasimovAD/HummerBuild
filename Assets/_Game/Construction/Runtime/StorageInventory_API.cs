using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реализация склада под старый проект, но с публичным API:
///   GetAmount(res), AddItem(res, amount), RemoveItem(res, amount), OnChanged(res, delta)
/// Основа — свой словарь; лимиты: по слотам и по весу (необязательно).
/// </summary>
public partial class StorageInventory : InventoryProvider, ISerializationCallbackReceiver
{
    [Header("Identity")]
    public string ProviderId = "MainStorage";

    [Header("Limits")]
    [Tooltip("Максимум уникальных типов в инвентаре (0 = без лимита).")]
    public int Slots = 50;

    [Tooltip("Максимальный общий вес, кг (0 = без лимита).")]
    public float MaxKg = 250f;

    [Header("Вес ресурсов (опционально)")]
    public List<ResourceWeight> Weights = new();
    public float DefaultUnitKg = 1f;

    [Serializable]
    public class ResourceWeight
    {
        public ScriptableObject resource;
        public float unitKg = 1f;
    }

    // ===== Runtime-хранилище =====
    // ключ — твой старый ScriptableObject ресурса (cement/log/sand/...)
    private Dictionary<ScriptableObject, int> _dict =
        new(ReferenceEqualityComparer<ScriptableObject>.Default);

    // ===== Сериализация для инспектора (видеть содержимое) =====
    [SerializeField] private List<Stack> _serialized = new();

    [Serializable]
    public class Stack
    {
        public ScriptableObject resource;
        public int amount;
    }

    /// <summary>Событие: подписчики (TopBar/Display/и т.п.) могут реагировать на изменения.</summary>
    public event Action<ScriptableObject, int> OnChanged;

    // ===== Публичный API (то, что зовёт адаптер) =====

    /// <summary>Текущее количество по ресурсу.</summary>
    public int GetAmount(ScriptableObject res)
    {
        if (!res) return 0;
        return _dict.TryGetValue(res, out var v) ? v : 0;
    }

    /// <summary>Добавить ресурс. Возвращает фактически добавленное количество.</summary>
    public int AddItem(ScriptableObject res, int amount)
    {
        if (!res || amount <= 0) return 0;

        // Лимит по слотам
        bool hasKey = _dict.ContainsKey(res);
        if (!hasKey && Slots > 0 && _dict.Count >= Slots)
            return 0;

        // Лимит по весу
        int canByWeight = amount;
        if (MaxKg > 0f)
        {
            float unit = GetUnitKg(res);
            float free = Mathf.Max(0f, MaxKg - GetTotalWeight());
            int cap = unit > 0f ? Mathf.FloorToInt(free / unit) : amount;
            canByWeight = Mathf.Clamp(cap, 0, amount);
        }

        int toAdd = Mathf.Clamp(canByWeight, 0, amount);
        if (toAdd <= 0) return 0;

        int cur = GetAmount(res);
        int after = cur + toAdd;
        _dict[res] = after;

        SyncSerialized();
        SafeInvokeChanged(res, +toAdd);
        return toAdd;
    }

    /// <summary>Снять ресурс. Возвращает фактически снятое количество.</summary>
    public int RemoveItem(ScriptableObject res, int amount)
    {
        if (!res || amount <= 0) return 0;

        int cur = GetAmount(res);
        int take = Mathf.Clamp(amount, 0, cur);
        if (take <= 0) return 0;

        int after = cur - take;
        if (after == 0) _dict.Remove(res);
        else _dict[res] = after;

        SyncSerialized();
        SafeInvokeChanged(res, -take);
        return take;
    }

    /// <summary>DEV: установить абсолютное значение (для сидов/тестов). Возвращает дельту.</summary>
    public int DevSet(ScriptableObject res, int absoluteAmount)
    {
        if (!res || absoluteAmount < 0) return 0;
        int before = GetAmount(res);
        if (absoluteAmount == 0) _dict.Remove(res);
        else _dict[res] = absoluteAmount;

        SyncSerialized();
        SafeInvokeChanged(res, absoluteAmount - before);
        return absoluteAmount - before;
    }

    // ===== Утилиты =====

    float GetUnitKg(ScriptableObject res)
    {
        if (!res) return DefaultUnitKg;
        foreach (var w in Weights)
            if (w.resource == res)
                return Mathf.Max(0f, w.unitKg);
        return Mathf.Max(0f, DefaultUnitKg);
    }

    float GetTotalWeight()
    {
        if (_dict.Count == 0) return 0f;
        float sum = 0f;
        foreach (var kv in _dict)
            sum += GetUnitKg(kv.Key) * kv.Value;
        return sum;
    }

    void SafeInvokeChanged(ScriptableObject res, int delta)
    {
        try { OnChanged?.Invoke(res, delta); }
        catch { /* ignore */ }
    }

    void SyncSerialized()
    {
        _serialized.Clear();
        foreach (var kv in _dict)
            _serialized.Add(new Stack { resource = kv.Key, amount = kv.Value });
    }

    // ===== Сериализация =====
    public void OnBeforeSerialize() => SyncSerialized();

    public void OnAfterDeserialize()
    {
        _dict.Clear();
        foreach (var s in _serialized)
        {
            if (!s.resource || s.amount <= 0) continue;
            if (_dict.TryGetValue(s.resource, out var cur)) _dict[s.resource] = cur + s.amount;
            else _dict[s.resource] = s.amount;
        }
    }

    // Компаратор по ссылке (важно для разных SO с одинаковыми данными)
    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Default = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
