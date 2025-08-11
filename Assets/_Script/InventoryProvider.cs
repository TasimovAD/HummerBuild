using UnityEngine;

public abstract class InventoryProvider : MonoBehaviour, IInventoryProvider
{
    [SerializeField] private string providerId;
    public string ProviderId => string.IsNullOrEmpty(providerId) ? name : providerId;

    [Header("Limits")]
    public int slots = 12;
    public float maxKg = 100f;

    public Inventory Inventory { get; private set; }

    protected virtual void Awake()    { EnsureInit(); }
    protected virtual void OnEnable() { EnsureInit(); }

    private void EnsureInit()
    {
        if (Inventory == null)
            Inventory = new Inventory { maxSlots = slots, maxWeightKg = maxKg };
    }
}