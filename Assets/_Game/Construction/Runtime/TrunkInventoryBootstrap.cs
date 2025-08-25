using UnityEngine;

public class TrunkInventoryBootstrap : MonoBehaviour
{
    [Header("Палеты в багажнике")]
    public PalletInteractable TrunkPallet;
    public InventoryProviderAdapter TrunkInventory;

    private void Awake()
    {
        // Линкуем, чтобы добавление/удаление штук отражалось на слоты и обратно
        TrunkPallet.linkedInventory = TrunkInventory; // поле уже есть у PalletInteractable 
    }
}
