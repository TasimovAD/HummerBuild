using System;
using UnityEngine;

[Serializable]
public struct ResourceStack {
    public ResourceType type;
    public int amount;
    public ResourceStack(ResourceType t, int a){ type = t; amount = a; }
    public float TotalKg => (type ? type.kgPerUnit : 0f) * amount;
}
