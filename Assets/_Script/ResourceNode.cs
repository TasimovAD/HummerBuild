using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceNode : MonoBehaviour {
    public ResourceType outputType;
    public int batchAmount = 5;
    public float harvestTime = 2f;
    public int maxBatches = 20; // 0 = бесконечно

    private int _done;
    private bool _busy;

    private void Reset(){ GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerStay(Collider other){
        var charInv = other.GetComponentInParent<CharacterInventory>();
        if (!charInv || _busy) return;
        if (Input.GetKey(KeyCode.F)) { StartCoroutine(Harvest(charInv)); }
    }

    IEnumerator Harvest(InventoryProvider target){
        _busy = true;
        yield return new WaitForSeconds(harvestTime);
        if (maxBatches==0 || _done<maxBatches){
            target.Inventory.Add(outputType, batchAmount);
            _done++;
        }
        _busy = false;
    }
}
