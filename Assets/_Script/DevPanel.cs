using UnityEngine;

public class DevPanel : MonoBehaviour {
    public ResourceRegistry registry;
    string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public void Save() => SaveLoadService.Save(Path, registry);
    public void Load() => SaveLoadService.Load(Path, registry);
}
