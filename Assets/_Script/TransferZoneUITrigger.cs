using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TransferZoneUITrigger : MonoBehaviour {
    public InventoryProvider fromProvider;
    public InventoryProvider toProvider;
    public GameObject panel; // UI панель с выбором типа/кол-ва

    private void Reset(){ GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other){
        if (!other.GetComponentInParent<CharacterInventory>()) return;
        if (panel) {
            panel.SetActive(true);
            var ui = panel.GetComponent<TransferPanelUI>();
            if (ui) ui.Bind(fromProvider, toProvider);
        }
    }
    private void OnTriggerExit(Collider other){
        if (!other.GetComponentInParent<CharacterInventory>()) return;
        if (panel) panel.SetActive(false);
    }
}