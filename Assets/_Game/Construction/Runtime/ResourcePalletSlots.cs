using UnityEngine;
using System.Collections.Generic;

public class ResourcePalletSlots : MonoBehaviour
{
    [Header("Настройки")]
    public GameObject defaultPrefab;
    public Transform slotRoot;           // 🔹 Указатель на корень слотов
    public bool autoFindSlots = true;

    private List<Transform> slots = new();
    private Dictionary<Transform, GameObject> placed = new(); // слот → объект

    void Awake()
    {
        if (autoFindSlots)
            FindSlots();
    }

    public void FindSlots()
    {
        slots.Clear();
        if (!slotRoot)
        {
            Debug.LogError($"{name}: Не назначен slotRoot!", this);
            return;
        }

        foreach (Transform child in slotRoot)
        {
            if (child.name.ToLower().StartsWith("slot"))
                slots.Add(child);
        }
    }

    public bool TryAdd(GameObject prefab)
    {
        foreach (var slot in slots)
        {
            if (!placed.ContainsKey(slot) || placed[slot] == null)
            {
                GameObject obj = Instantiate(prefab, slot.position, slot.rotation, slot);
                obj.name = prefab.name;
                placed[slot] = obj;
                return true;
            }
        }
        return false;
    }

    public GameObject Take()
    {
        foreach (var slot in slots)
        {
            if (placed.TryGetValue(slot, out var obj) && obj != null)
            {
                placed[slot] = null;
                obj.transform.SetParent(null); // отсоединить
                return obj;
            }
        }
        return null;
    }

    public void ClearAll()
    {
        foreach (var pair in placed)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }
        placed.Clear();
    }

    public int Count => placed.Count;
}
