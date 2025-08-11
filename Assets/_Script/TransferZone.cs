using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TransferZone : MonoBehaviour {
    [Header("Откуда → Куда")]
    public InventoryProvider fromProvider;
    public InventoryProvider toProvider;

    [Header("Что переносим")]
    public ResourceType type;      // для MVP фиксируем тип
    public int amount = 9999;      // "всё"

    private void Reset(){ GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerStay(Collider other){
        // Только если внутри стоит персонаж
        if (!other.GetComponentInParent<CharacterInventory>()) return;
        if (Input.GetKeyDown(KeyCode.R) && type) {
            var moved = Inventory.Transfer(fromProvider.Inventory, toProvider.Inventory, type, amount);
            Debug.Log($"[TransferZone] Moved {moved} x {type.id}");
        }
    }
}
