using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory {
    public event Action OnChanged;
    public readonly List<ResourceStack> stacks = new();
    public int maxSlots = 12;
    public float maxWeightKg = 200f;

    public float CurrentWeightKg {
        get { float s = 0; foreach (var st in stacks) s += st.TotalKg; return s; }
    }

    public int GetAmount(ResourceType t){ int s=0; foreach (var st in stacks) if (st.type==t) s+=st.amount; return s; }

    public bool CanAdd(ResourceType t, int amount) {
        if (!t || amount <= 0) return false;
        if (CurrentWeightKg + t.kgPerUnit * amount > maxWeightKg) return false;
        bool has = stacks.Exists(s => s.type == t);
        if (!has && stacks.Count >= maxSlots) return false;
        return true;
    }

    public int Add(ResourceType t, int amount){
        if (!CanAdd(t, amount)) return 0;
        for (int i=0;i<stacks.Count;i++){
            if (stacks[i].type != t) continue;
            int move = Mathf.Min(amount, t.stackLimit - stacks[i].amount);
            stacks[i] = new ResourceStack(t, stacks[i].amount + move);
            amount -= move;
            if (amount<=0){ OnChanged?.Invoke(); return move; }
        }
        int toAdd = Mathf.Min(amount, t.stackLimit);
        stacks.Add(new ResourceStack(t, toAdd));
        OnChanged?.Invoke();
        return toAdd;
    }

    public int Remove(ResourceType t, int amount){
        int removed=0;
        for(int i=stacks.Count-1;i>=0 && amount>0;i--){
            if (stacks[i].type != t) continue;
            int take = Mathf.Min(amount, stacks[i].amount);
            stacks[i] = new ResourceStack(t, stacks[i].amount - take);
            amount -= take; removed += take;
            if (stacks[i].amount<=0) stacks.RemoveAt(i);
        }
        if (removed>0) OnChanged?.Invoke();
        return removed;
    }

    public static int Transfer(Inventory from, Inventory to, ResourceType t, int amount){
        int can = Mathf.Min(amount, from.GetAmount(t));
        int moved=0;
        while (moved<can && to.CanAdd(t,1)){
            to.Add(t,1);
            from.Remove(t,1);
            moved++;
        }
        return moved;
    }
}
