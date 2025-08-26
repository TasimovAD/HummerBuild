using UnityEngine;

/// <summary>
/// Расширение VehicleInventory с дополнительными методами для работы через InventoryProviderAdapter
/// </summary>
public static class VehicleInventoryExtensions
{
    /// <summary>
    /// Получить количество ресурса (для адаптера) - преобразует ResourceDef в ResourceType
    /// </summary>
    public static int Get(this VehicleInventory vehicle, ResourceDef resource)
    {
        if (!vehicle || !resource || vehicle.Inventory == null)
            return 0;

        // Нужно преобразовать ResourceDef в ResourceType через mapping
        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return vehicle.Inventory.GetAmount(resourceType);
    }

    /// <summary>
    /// Добавить ресурс (для адаптера) - преобразует ResourceDef в ResourceType
    /// </summary>
    public static int Add(this VehicleInventory vehicle, ResourceDef resource, int amount)
    {
        if (!vehicle || !resource || vehicle.Inventory == null || amount <= 0)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return vehicle.Inventory.Add(resourceType, amount);
    }

    /// <summary>
    /// Удалить ресурс (для адаптера) - преобразует ResourceDef в ResourceType
    /// </summary>
    public static int Remove(this VehicleInventory vehicle, ResourceDef resource, int amount)
    {
        if (!vehicle || !resource || vehicle.Inventory == null || amount <= 0)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return vehicle.Inventory.Remove(resourceType, amount);
    }

    /// <summary>
    /// Преобразует ResourceDef в ResourceType (нужно настроить под вашу систему)
    /// </summary>
    private static ResourceType ConvertToResourceType(ResourceDef resourceDef)
    {
        if (!resourceDef) return null;

        // Вариант 1: Поиск по ID/имени
        var allResourceTypes = Resources.FindObjectsOfTypeAll<ResourceType>();
        foreach (var rt in allResourceTypes)
        {
            if (rt.name == resourceDef.Id || rt.displayName == resourceDef.DisplayName)
                return rt;
        }

        // Вариант 2: Используем ResourceRegistry если есть
        var registry = Object.FindObjectOfType<ResourceRegistry>();
        if (registry && registry.all != null)
        {
            foreach (var rt in registry.all)
            {
                if (rt.name == resourceDef.Id || rt.displayName == resourceDef.DisplayName)
                    return rt;
            }
        }

        Debug.LogWarning($"[VehicleInventoryExtensions] Не найден ResourceType для ResourceDef: {resourceDef.DisplayName}");
        return null;
    }
}

/// <summary>
/// Улучшенная версия VehicleInventory с поддержкой адаптера
/// </summary>
public class VehicleInventoryImproved : VehicleInventory
{
    /// <summary>
    /// Получить количество ресурса (для адаптера) - работает с ResourceDef
    /// </summary>
    public int Get(ResourceDef resource)
    {
        if (!resource || Inventory == null)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return Inventory.GetAmount(resourceType);
    }

    /// <summary>
    /// Добавить ресурс (для адаптера) - работает с ResourceDef
    /// </summary>
    public int Add(ResourceDef resource, int amount)
    {
        if (!resource || Inventory == null || amount <= 0)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return Inventory.Add(resourceType, amount);
    }

    /// <summary>
    /// Удалить ресурс (для адаптера) - работает с ResourceDef
    /// </summary>
    public int Remove(ResourceDef resource, int amount)
    {
        if (!resource || Inventory == null || amount <= 0)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        return Inventory.Remove(resourceType, amount);
    }

    /// <summary>
    /// Преобразует ResourceDef в ResourceType
    /// </summary>
    private ResourceType ConvertToResourceType(ResourceDef resourceDef)
    {
        if (!resourceDef) return null;

        // Поиск по имени/ID
        var allResourceTypes = Resources.FindObjectsOfTypeAll<ResourceType>();
        foreach (var rt in allResourceTypes)
        {
            if (rt.name == resourceDef.Id || rt.displayName == resourceDef.DisplayName)
                return rt;
        }

        // Через реестр
        var registry = FindObjectOfType<ResourceRegistry>();
        if (registry && registry.all != null)
        {
            foreach (var rt in registry.all)
            {
                if (rt.name == resourceDef.Id || rt.displayName == resourceDef.DisplayName)
                    return rt;
            }
        }

        Debug.LogWarning($"[VehicleInventoryImproved] Не найден ResourceType для ResourceDef: {resourceDef.DisplayName}");
        return null;
    }

    /// <summary>
    /// Получить общий вес багажника
    /// </summary>
    public float GetCurrentWeight()
    {
        return Inventory?.CurrentWeightKg ?? 0f;
    }

    /// <summary>
    /// Получить процент заполнения багажника (0-1)
    /// </summary>
    public float GetFillPercentage()
    {
        if (Inventory == null) return 0f;
        
        float weightPercent = maxKg > 0 ? Inventory.CurrentWeightKg / maxKg : 0f;
        float slotPercent = slots > 0 ? (float)Inventory.stacks.Count / slots : 0f;
        
        return Mathf.Max(weightPercent, slotPercent);
    }

    /// <summary>
    /// Проверить, можно ли добавить ресурс
    /// </summary>
    public bool CanAdd(ResourceDef resource, int amount = 1)
    {
        if (!resource || Inventory == null || amount <= 0)
            return false;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return false;

        return Inventory.CanAdd(resourceType, amount);
    }

    /// <summary>
    /// Получить максимальное количество ресурса, которое можно добавить
    /// </summary>
    public int GetMaxCanAdd(ResourceDef resource)
    {
        if (!resource || Inventory == null)
            return 0;

        ResourceType resourceType = ConvertToResourceType(resource);
        if (!resourceType) return 0;

        // Простая проверка по слотам (можно улучшить)
        if (Inventory.stacks.Count >= slots)
        {
            // Если ресурс уже есть - можем добавить больше в существующий стек
            var existing = Inventory.stacks.Find(s => s.type == resourceType);
            if (existing.type != null) // исправлено: проверяем type вместо всего объекта
            {
                // Тут можно добавить логику лимитов на стек
                return int.MaxValue; // Пока без ограничений
            }
            return 0; // Нет свободных слотов
        }

        return int.MaxValue; // Есть свободные слоты
    }

    #if UNITY_EDITOR
    [ContextMenu("DEBUG/Print Inventory State")]
    void DebugPrintState()
    {
        if (Inventory == null)
        {
            Debug.Log($"[{name}] Inventory is NULL");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[{name}] Vehicle Inventory State:");
        sb.AppendLine($"Slots: {Inventory.stacks.Count}/{slots}");
        sb.AppendLine($"Weight: {Inventory.CurrentWeightKg:F1}/{maxKg} kg");
        sb.AppendLine($"Fill: {GetFillPercentage() * 100:F1}%");
        sb.AppendLine("Resources:");
        
        foreach (var stack in Inventory.stacks)
        {
            if (stack.type != null)
                sb.AppendLine($"  - {stack.type.displayName}: {stack.amount}"); // исправлено: displayName вместо DisplayName
        }

        Debug.Log(sb.ToString());
    }

    [ContextMenu("DEBUG/Clear All")]
    void DebugClearAll()
    {
        if (Inventory != null)
        {
            Inventory.stacks.Clear();
            Debug.Log($"[{name}] Inventory cleared");
        }
    }
    #endif
}