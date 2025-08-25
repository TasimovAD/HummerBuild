using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PalletUseZone : MonoBehaviour
{
    public PalletInteractable Pallet;             // палета склада/магазина/багажника
    public PlayerCarryController PlayerCarry;     // переносчик игрока

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Input.GetKeyDown(KeyCode.E))         // Взять если руки пусты
        {
            if (!PlayerCarry.IsCarrying && Pallet.TryTakeOne(out GameObject prop)) // 
                PlayerCarry.Attach(prop);                                           // 
        }
        else if (Input.GetKeyDown(KeyCode.F))    // Положить если что‑то в руках
        {
            if (PlayerCarry.IsCarrying)
            {
                var prop = PlayerCarry.Detach();                                    // 
                if (!Pallet.TryPutOne(prop))                                        // 
                    PlayerCarry.Attach(prop); // если не получилось — вернуть в руки
            }
        }
    }
}
