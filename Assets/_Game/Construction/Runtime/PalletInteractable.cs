using UnityEngine;
using TMPro;

/// Вешается на палету. Дроп/пикап только с совпадением типа ресурса (если strictMatch = true).
/// При попытке положить не тот ресурс выводит предупреждение в warningText.
[RequireComponent(typeof(ResourcePalletSlots))]
public class PalletInteractable : MonoBehaviour
{
    [Header("Связи")]
    public ResourcePalletSlots slots;                 // визуальные слоты этой палеты
    public InventoryProviderAdapter linkedInventory;  // инвентарь, который отражает содержимое палеты

    [Header("Правила")]
    public bool strictMatch = true;                   // true = принимать только тот ресурс, который назначен палете

    [Header("UI предупреждения (опц.)")]
    public TextMeshProUGUI warningText;              // поле для вывода "Тут хранится другой ресурс"
    public float warningLifetime = 2.0f;             // через сколько секунд очистить сообщение

    float _warnUntil = -1f;

    void Reset()
    {
        slots = GetComponent<ResourcePalletSlots>();
    }

    void Update()
    {
        // авто-очистка предупреждения
        if (warningText && _warnUntil > 0f && Time.unscaledTime >= _warnUntil)
        {
            warningText.text = "";
            _warnUntil = -1f;
        }
    }

    /// Взять один предмет со слотов (и из инвентаря)
    public bool TryTakeOne(out GameObject prop)
    {
        prop = null;
        if (!slots) return false;

        // Визуально
        var go = slots.Take();
        if (!go) return false;

        // Какой ресурс забираем (по тегу пропа; если его нет — по типу палеты)
        var tag = go.GetComponentInChildren<CarryPropTag>();
        var res = tag ? tag.resource : (slots ? slots.Resource : null);

        // ВАЖНО: сначала открепляем объект от палеты, потом трогаем инвентарь
        // Это предотвращает конфликт с PalletGroupManager.RebuildAll
        go.transform.SetParent(null, true);

        // Снимаем 1 из инвентаря (если есть привязка)
        if (linkedInventory && res)
            linkedInventory.Remove(res, 1);

        prop = go;

        // очистим сообщение, если было
        ClearWarning();
        return true;
    }

    /// Положить один предмет из рук на палету
    /// Возвращает true, если удалось уложить в слот; false — если отклонено (объект остаётся в руках у игрока).
    public bool TryPutOne(GameObject prop)
    {
        if (!prop || !slots) return false;

        // 1) Определяем ресурс из переносимого префаба
        var tag = prop.GetComponentInChildren<CarryPropTag>();
        var resFromProp = tag ? tag.resource : null;

        // 2) Ресурс палеты (если задан)
        var palletRes = slots.Resource;

        // 3) Проверка соответствия (строгое совпадение)
        if (strictMatch && palletRes && resFromProp && palletRes != resFromProp)
        {
            ShowWarning("Тут хранится другой ресурс");
            return false; // объект остаётся в руках у игрока
        }

        // Если у пропа нет тега — считаем, что кладём тип палеты
        var finalRes = resFromProp ? resFromProp : palletRes;

        if (!finalRes)
        {
            ShowWarning("Не удалось определить тип ресурса");
            return false;
        }

        // === ВАЖНО: сначала кладём визуально, потом трогаем инвентарь ===
        if (!slots.TryAdd(prop))
        {
            ShowWarning("Нет свободных мест на палете");
            return false;
        }

        // убираем физику, чтобы не падало
        if (prop.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
        foreach (var c in prop.GetComponentsInChildren<Collider>()) c.enabled = false;

        // Теперь корректируем инвентарь (если привязан)
        if (linkedInventory)
            linkedInventory.Add(finalRes, 1);

        ClearWarning();
        return true;
    }

    // ==== Вспомогательное ====
    void ShowWarning(string msg)
    {
        if (!warningText) return;
        warningText.text = msg;
        _warnUntil = Time.unscaledTime + Mathf.Max(0.1f, warningLifetime);
    }

    void ClearWarning()
    {
        if (!warningText) return;
        warningText.text = "";
        _warnUntil = -1f;
    }
}
