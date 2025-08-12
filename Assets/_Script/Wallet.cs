using System;
using UnityEngine;

public class Wallet : MonoBehaviour {
    [SerializeField] private long startingAmount = 0;
    public long Amount { get; private set; }

    public event Action<long> OnChanged;

    void Awake() {
        Amount = startingAmount;
        OnChanged?.Invoke(Amount);
    }

    public bool CanSpend(long value) => value >= 0 && Amount >= value;

    public void Add(long value) {
        if (value <= 0) return;
        Amount += value;
        OnChanged?.Invoke(Amount);
    }

    public bool Spend(long value) {
        if (!CanSpend(value)) return false;
        Amount -= value;
        OnChanged?.Invoke(Amount);
        return true;
    }

    public void Set(long value) {
        Amount = Mathf.Max(0, (int)value);
        OnChanged?.Invoke(Amount);
    }
}
