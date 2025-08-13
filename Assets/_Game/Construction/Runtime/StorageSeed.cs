// StorageSeed.cs
using UnityEngine;

[System.Serializable]
public class SeedItem { public ScriptableObject legacyRes; public int amount = 0; }

public class StorageSeed : MonoBehaviour
{
    public StorageInventory storage;   // СЮДА: StorageInventory со склада
    public SeedItem[] items;
    public bool autoSeedOnStart = false;   // выключено по умолчанию
    bool _seeded;

    [ContextMenu("Seed Now (Play mode)")]
    public void SeedNow()
    {
        if (!storage) { Debug.LogError("[Seed] storage == null", this); return; }
        int total = 0;
        foreach (var it in items)
        {
            if (!it.legacyRes || it.amount <= 0) continue;
            total += storage.AddItem(it.legacyRes, it.amount);
        }
        _seeded = true;
        Debug.Log($"[Seed] Done. Total added={total}");
    }

    void Start()
    {
        if (autoSeedOnStart && !_seeded)
            SeedNow();
    }
}
