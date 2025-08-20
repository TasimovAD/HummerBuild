using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    [Header("Куда крепим визуал в руках")]
    public Transform handSocket;

    public ResourceDef CarriedRes { get; private set; }
    public GameObject CarriedGO { get; private set; }

    public bool IsEmpty => CarriedRes == null;

    /// Поднять 1 ед. ресурса: создаём визуал в руке
    public bool Pickup(ResourceDef res)
    {
        if (!IsEmpty) return false;
        if (!res) return false;

        GameObject prefab = res.CarryProp;
        if (!prefab)
        {
            Debug.LogWarning($"[PlayerCarry] У ресурса {res?.Id} не задан CarryProp. Визуал в руке не появится.");
        }

        CarriedRes = res;

        if (prefab && handSocket)
        {
            CarriedGO = Object.Instantiate(prefab, handSocket);
            CarriedGO.transform.localPosition = Vector3.zero;
            CarriedGO.transform.localRotation = Quaternion.identity;
        }

        return true;
    }

    /// Сбросить из рук (без помещения в инвентарь)
    public void ClearHand()
    {
        if (CarriedGO) Object.Destroy(CarriedGO);
        CarriedGO = null;
        CarriedRes = null;
    }
}
