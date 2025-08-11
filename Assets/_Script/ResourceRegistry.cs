using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Resource Registry")]
public class ResourceRegistry : ScriptableObject {
    public List<ResourceType> all = new();
    private Dictionary<string, ResourceType> _map;

    public ResourceType GetById(string id) {
        _map ??= Build();
        return _map.TryGetValue(id, out var t) ? t : null;
    }
    private Dictionary<string, ResourceType> Build() {
        var d = new Dictionary<string, ResourceType>();
        foreach (var t in all) if (t && !string.IsNullOrEmpty(t.id)) d[t.id] = t;
        return d;
    }
}
