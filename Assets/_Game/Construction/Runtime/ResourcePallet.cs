using UnityEngine;
using System.Collections.Generic;

public class ResourcePallet : MonoBehaviour
{
    [Header("Тип ресурса")]
    public ResourceDef resource;

    [Header("Настройки")]
    public int maxCapacity = 50;
    public Transform spawnRoot;
    public Vector3 spacing = new Vector3(0.3f, 0, 0.3f); // сетка

    private List<GameObject> spawned = new();

    public void Rebuild(int count, GameObject prefab)
    {
        Clear();

        for (int i = 0; i < Mathf.Min(count, maxCapacity); i++)
        {
            Vector3 offset = new Vector3((i % 5) * spacing.x, 0, (i / 5) * spacing.z);
            var go = Instantiate(prefab, spawnRoot.position + offset, Quaternion.identity, spawnRoot);
            go.name = $"{resource.Id}_carry_{i}";
            spawned.Add(go);
        }
    }

    public GameObject TakeOne()
    {
        if (spawned.Count == 0) return null;

        var go = spawned[0];
        spawned.RemoveAt(0);
        go.transform.SetParent(null); // открепить для переноса
        return go;
    }

    public void Clear()
    {
        foreach (var go in spawned)
            if (go) Destroy(go);
        spawned.Clear();
    }

    public int CurrentCount => spawned.Count;
}
