using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourcePickup : MonoBehaviour {
    public ResourceType type;
    public int amount = 5;
    public InventoryProvider forceProvider; // опционально

    private void Reset(){ GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerStay(Collider other){
        var prov = forceProvider ? forceProvider : other.GetComponentInParent<InventoryProvider>();
        if (!prov) return;
        if (Input.GetKeyDown(KeyCode.E)) TryPickup(prov); // для мобилки повесь UI и вызывай TryPickup()
    }

    public bool TryPickup(InventoryProvider provider){
        if (!type || amount<=0) return false;
        int added = provider.Inventory.Add(type, amount);
        if (added>0){ Destroy(gameObject); return true; }
        return false;
    }
}
