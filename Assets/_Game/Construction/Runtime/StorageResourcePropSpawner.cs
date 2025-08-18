using UnityEngine;
using System.Collections.Generic;

public class StorageResourcePropSpawner : MonoBehaviour
{
    [Header("Ссылки")]
    public InventoryProviderAdapter inventory;     // Складской инвентарь
    public Transform spawnRoot;                    // Куда спавнятся объекты
    public GameObject defaultPrefab;               // Если у ресурса нет CarryProp

    [Header("Ресурсы для отображения")]
    public List<ResourceDef> resources = new();    // Ручной список ресурсов

    [Header("Визуализация")]
    public float spacing = 0.4f;

    private Dictionary<ResourceDef, List<GameObject>> spawned = new();

    void Start()
    {
        if (inventory != null)
            inventory.OnChanged += Rebuild;

        Rebuild();
    }

    void Rebuild(ResourceDef _) => Rebuild();

    void Rebuild()
    {
        if (inventory == null || spawnRoot == null) return;

        foreach (var res in resources)
        {
            int desiredCount = inventory.Get(res);
            if (!spawned.TryGetValue(res, out var list))
            {
                list = new List<GameObject>();
                spawned[res] = list;
            }

            int currentCount = list.Count;
            int diff = desiredCount - currentCount;

            if (diff > 0)
            {
                // Добавить недостающие
                for (int i = 0; i < diff; i++)
                {
                    Vector3 offset = new Vector3(((list.Count + i) % 5) * spacing, 0, ((list.Count + i) / 5) * spacing);
                    GameObject prefab = res.CarryProp ?? defaultPrefab;
                    if (!prefab) continue;

                    var go = Instantiate(prefab, spawnRoot.position + offset, Quaternion.identity, spawnRoot);
                    go.name = $"{res.Id}_prop_{list.Count + i}";
                    list.Add(go);
                }
            }
            else if (diff < 0)
            {
                // Удалить лишние
                int toRemove = -diff;
                for (int i = 0; i < toRemove && list.Count > 0; i++)
                {
                    var last = list[list.Count - 1];
                    if (last) Destroy(last);
                    list.RemoveAt(list.Count - 1);
                }
            }
        }
    }

    /// <summary>
    /// Забираем 3D-префаб конкретного ресурса и удаляем его из склада.
    /// </summary>
    public GameObject TakeProp(ResourceDef res)
    {
        if (!inventory || !res) return null;

        if (spawned.TryGetValue(res, out var list) && list.Count > 0)
        {
            var go = list[0];
            list.RemoveAt(0);
            inventory.Remove(res, 1); // Снимаем 1 ресурс из инвентаря
            go.transform.SetParent(null); // открепляем от склада
            return go; // отдаём физический объект
        }

        return null;
    }
}
