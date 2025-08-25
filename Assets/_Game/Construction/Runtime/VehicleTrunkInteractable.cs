using UnityEngine;

/// Багажник/кузов. Принимает ЛЮБОЙ ресурс.
/// Требуется: InventoryProviderAdapter на объекте (или укажи ссылку вручную).
[RequireComponent(typeof(InventoryProviderAdapter))]
public class VehicleTrunkInteractable : MonoBehaviour
{
    [Header("Инвентарь багажника")]
    public InventoryProviderAdapter trunkInventory; // если null — возьмём с этого объекта

    void Awake()
    {
        if (!trunkInventory) trunkInventory = GetComponent<InventoryProviderAdapter>();
        if (!trunkInventory)
            Debug.LogError("[VehicleTrunkInteractable] Нет InventoryProviderAdapter!", this);
    }

    /// Положить один предмет из рук в багажник (любой ресурс).
    /// Возвращает true, если предмет принят (его надо удалить из рук).
    public bool TryPutOne(GameObject prop)
    {
        if (!prop || !trunkInventory) return false;

        // 1) Пытаемся вытащить тип ресурса из CarryPropTag
        var tag = prop.GetComponentInChildren<CarryPropTag>();
        var res = tag ? tag.resource : null;

        if (!res)
        {
            Debug.LogWarning("[VehicleTrunkInteractable] Не удалось определить ResourceDef у принесённого пропа.");
            return false;
        }

        // 2) Добавляем 1 в инвентарь
        int added = trunkInventory.Add(res, 1);
        if (added <= 0) return false;

        // 3) Поскольку теперь это ресурс в инвентаре — сам проп должен быть уничтожен
        return true;
    }

    /// Взять один предмет из багажника.
    /// Возвращает сгенерированный GameObject пропа (будет передан в руку игроку).
    public bool TryTakeOne(out GameObject propOut)
    {
        propOut = null;
        if (!trunkInventory) return false;

        // 1) Находим ЛЮБОЙ ресурс, которого > 0
        ResourceDef found = null;
        int foundCount = 0;

        // Минимально инвазивно: пробежимся по всем ресурсам, зарегистрированным в проекте
        var allRes = Resources.FindObjectsOfTypeAll<ResourceDef>();
        foreach (var r in allRes)
        {
            int c = trunkInventory.Get(r);
            if (c > 0) { found = r; foundCount = c; break; }
        }

        if (!found || foundCount <= 0) return false;

        // 2) Вычитаем 1 из инвентаря
        int removed = trunkInventory.Remove(found, 1);
        if (removed <= 0) return false;

        // 3) Спаун пропа
        var prefab = found.CarryProp;
        if (!prefab)
        {
            Debug.LogWarning($"[VehicleTrunkInteractable] У ресурса {found?.Id} не задан CarryProp — отдать нечего.");
            return false;
        }

        var go = Instantiate(prefab);
        // Гарантируем тег
        var tag = go.GetComponent<CarryPropTag>();
        if (!tag) tag = go.AddComponent<CarryPropTag>();
        tag.resource = found;

        propOut = go;
        return true;
    }
}
