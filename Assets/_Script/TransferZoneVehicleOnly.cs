using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class TransferZoneVehicleOnly : MonoBehaviour
{
    [Header("Откуда → Куда")]
    public VehicleInventory fromVehicle;     // конкретная машина (опционально)
    public StorageInventory toStorage;       // склад (обязательно)

    [Header("Что переносим")]
    public ResourceType type;                // если null — выгружаем все типы
    public int amount = 9999;                // для конкретного типа ("всё")

    [Header("Поведение")]
    public bool matchSpecificVehicle = true; // true: принимать только fromVehicle; false: любую машину с VehicleInventory

    [Header("UI")]
    public GameObject panel;                 // панель с кнопкой "Выгрузить"
    public Button unloadButton;              // сама кнопка

    private VehicleInventory _currentVehicle; // кто сейчас в зоне

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (unloadButton)
        {
            unloadButton.onClick.RemoveAllListeners();
            unloadButton.onClick.AddListener(Unload);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (TryGetVehicleInventory(other, out var vehicle))
        {
            if (matchSpecificVehicle && fromVehicle && vehicle != fromVehicle)
            {
                Debug.Log($"[StorageZone] Въехала другая машина: {vehicle.ProviderId}, ожидаем: {fromVehicle.ProviderId}");
                return;
            }

            _currentVehicle = vehicle;

            if (panel) panel.SetActive(true);
            Debug.Log($"[StorageZone] Машина въехала: {vehicle.ProviderId}. Нажмите 'Выгрузить'.");
            return;
        }

        // Игрок пешком — просто подсказка (без автовыгрузки)
        if (other.GetComponentInParent<CharacterInventory>())
        {
            Debug.Log("[StorageZone] Для выгрузки подъедьте на машине.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_currentVehicle && other.GetComponentInParent<VehicleInventory>() == _currentVehicle)
        {
            _currentVehicle = null;
            if (panel) panel.SetActive(false);
            Debug.Log("[StorageZone] Машина покинула зону склада.");
        }
    }

    /// <summary> Выгрузка по кнопке. </summary>
    public void Unload()
    {
        if (_currentVehicle == null)
        {
            Debug.Log("[StorageZone] Нет машины в зоне для выгрузки.");
            return;
        }
        if (toStorage == null)
        {
            Debug.LogWarning("[StorageZone] Не назначен toStorage (StorageInventory).");
            return;
        }

        int totalMoved = 0;
        if (type != null)
        {
            int moved = Inventory.Transfer(_currentVehicle.Inventory, toStorage.Inventory, type, amount);
            totalMoved += moved;
            Debug.Log($"[StorageZone] Unloaded {moved} x {type.id} -> {toStorage.ProviderId}");
        }
        else
        {
            // Выгрузить все типы
            var snapshot = new System.Collections.Generic.List<ResourceStack>(_currentVehicle.Inventory.stacks);
            foreach (var st in snapshot)
            {
                if (st.type == null || st.amount <= 0) continue;
                int moved = Inventory.Transfer(_currentVehicle.Inventory, toStorage.Inventory, st.type, st.amount);
                totalMoved += moved;
            }
            Debug.Log($"[StorageZone] Unloaded ALL types: {totalMoved} units -> {toStorage.ProviderId}");
        }
    }

    /// <summary> Ищем VehicleInventory на коллайдере/родителе/владельце Rigidbody. </summary>
    private bool TryGetVehicleInventory(Collider col, out VehicleInventory vi)
    {
        vi = col.GetComponent<VehicleInventory>();
        if (!vi) vi = col.GetComponentInParent<VehicleInventory>();
        if (!vi && col.attachedRigidbody)
            vi = col.attachedRigidbody.GetComponent<VehicleInventory>();
        return vi != null;
    }
}
