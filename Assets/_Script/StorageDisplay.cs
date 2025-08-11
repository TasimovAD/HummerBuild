using UnityEngine;

public class StorageDisplay : MonoBehaviour {
    public StorageInventory storage;
    public ResourceType type;

    [Header("Объекты уровней")]
    public GameObject level0; // пусто
    public GameObject level1; // немного
    public GameObject level2; // средне
    public GameObject level3; // много

    public int lvl1 = 10, lvl2 = 30, lvl3 = 60;

    private void OnEnable(){
        if (storage) storage.Inventory.OnChanged += Refresh;
        Refresh();
    }
    private void OnDisable(){
        if (storage) storage.Inventory.OnChanged -= Refresh;
    }

    public void Refresh(){
        int a = storage ? storage.Inventory.GetAmount(type) : 0;
        Set(level0, a <= 0);
        Set(level1, a > 0 && a <= lvl1);
        Set(level2, a > lvl1 && a <= lvl2);
        Set(level3, a > lvl2);
    }
    private void Set(GameObject go, bool v){ if (go) go.SetActive(v); }
}
