using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SaveLoadService {
    [System.Serializable] class ItemDTO { public string id; public int amount; }
    [System.Serializable] class InvDTO { public string providerId; public List<ItemDTO> items = new(); }
    [System.Serializable] class SiteDTO { public string siteName; public int stageIndex; }
    [System.Serializable] class GameDTO { public List<InvDTO> inventories = new(); public List<SiteDTO> sites = new(); }

    public static void Save(string path, ResourceRegistry registry){
        var data = new GameDTO();

        var provs = Object.FindObjectsByType<InventoryProvider>(FindObjectsSortMode.None);
        foreach (var p in provs){
            var inv = new InvDTO { providerId = p.ProviderId };
            foreach (var st in p.Inventory.stacks){
                if (!st.type) continue;
                inv.items.Add(new ItemDTO{ id = st.type.id, amount = st.amount });
            }
            data.inventories.Add(inv);
        }

        var sites = Object.FindObjectsByType<BuildingSite>(FindObjectsSortMode.None);
        foreach (var s in sites){
            data.sites.Add(new SiteDTO{ siteName = s.name, stageIndex = s.currentStageIndex });
        }

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"Saved to {path}");
    }

    public static void Load(string path, ResourceRegistry registry){
        if (!File.Exists(path)){ Debug.LogWarning("No save file"); return; }
        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<GameDTO>(json);

        var provs = Object.FindObjectsByType<InventoryProvider>(FindObjectsSortMode.None);
        foreach (var p in provs) p.Inventory.stacks.Clear();

        foreach (var inv in data.inventories){
            var p = provs.FirstOrDefault(x => x.ProviderId == inv.providerId);
            if (!p) continue;
            foreach (var it in inv.items){
                var type = registry.GetById(it.id);
                if (type) p.Inventory.Add(type, it.amount);
            }
        }

        var sites = Object.FindObjectsByType<BuildingSite>(FindObjectsSortMode.None);
        foreach (var s in sites){
            var dto = data.sites.FirstOrDefault(x => x.siteName == s.name);
            if (dto!=null) s.currentStageIndex = dto.stageIndex;
        }

        Debug.Log($"Loaded from {path}");
    }
}
