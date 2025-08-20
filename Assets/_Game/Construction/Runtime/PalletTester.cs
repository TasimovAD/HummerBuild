using UnityEngine;

public class PalletTester : MonoBehaviour {
    public ResourcePalletSlots palletSlots;

    void Start() {
        if (palletSlots != null) {
            palletSlots.Rebuild(10); // создаст 10 объектов в слотах
        }
    }
}
