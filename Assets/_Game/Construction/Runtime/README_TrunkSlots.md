# Система слотов багажника с ручной настройкой позиций

## Обзор

Новая система `VehicleTrunkSlots` позволяет точно указать, где должны располагаться ресурсы в багажнике машины, вместо автоматического размещения по матрице. Теперь каждый ресурс может быть размещен в конкретном, заранее заданном месте.

## Основные возможности

### 1. Ручная настройка позиций слотов
- Каждый слот имеет точную позицию, поворот и размер
- Слоты можно настроить в инспекторе Unity
- Поддержка индивидуальных цветов для каждого слота в редакторе

### 2. Физические коллайдеры слотов
- Каждый слот имеет свой коллайдер для физического взаимодействия
- Размер коллайдера настраивается индивидуально для каждого слота
- Коллайдеры работают как триггеры

### 3. Гибкая система размещения
- Размещение ресурса в конкретном слоте по индексу
- Автоматическое размещение в свободном слоте
- Проверка занятости слотов

## Настройка в инспекторе

### SimpleTrunkOnly
```csharp
[Header("Настройка слотов")]
public bool manualSlotConfiguration = true;        // Включить ручную настройку
public Vector2Int gridSize = new Vector2Int(4, 5); // Размер сетки (если не ручная)
public float slotSpacing = 0.5f;                  // Расстояние между слотами
public float slotHeight = 0.1f;                   // Высота слотов от дна
```

### VehicleTrunkSlots
```csharp
[Header("Ручная настройка слотов")]
public bool ManualSlotPositions = true;           // Включить ручную настройку
public List<SlotPosition> CustomSlotPositions;    // Список позиций слотов

[Header("Физика слотов")]
public bool AddSlotColliders = true;              // Добавить коллайдеры
public Vector3 SlotColliderSize = new Vector3(0.4f, 0.4f, 0.4f); // Размер по умолчанию
```

### SlotPosition (настройка каждого слота)
```csharp
public string slotName = "Slot";                  // Название слота
public Vector3 localPosition = Vector3.zero;      // Локальная позиция
public Vector3 localRotation = Vector3.zero;      // Локальный поворот
public Vector3 size = new Vector3(0.4f, 0.4f, 0.4f); // Размер коллайдера
public Color gizmoColor = Color.blue;             // Цвет в редакторе
```

## Использование в коде

### Размещение ресурса в конкретном слоте
```csharp
// Разместить ресурс в слоте с индексом 5
bool success = trunkSlots.PlaceResourceInSpecificSlot(gameObject, resourceDef, 5);

if (success)
{
    Debug.Log("Ресурс успешно размещен в слоте 5");
}
else
{
    Debug.Log("Слот 5 занят или не существует");
}
```

### Размещение ресурса в свободном слоте
```csharp
// Разместить ресурс в первом свободном слоте
bool success = trunkSlots.PlaceVisualObject(gameObject, resourceDef);

if (success)
{
    Debug.Log("Ресурс размещен в свободном слоте");
}
else
{
    Debug.Log("Нет свободных слотов");
}
```

### Получение информации о слоте
```csharp
// Получить данные слота по индексу
var slotData = trunkSlots.GetSlotData(3);
if (slotData != null)
{
    if (slotData.isEmpty)
    {
        Debug.Log("Слот 3 свободен");
    }
    else
    {
        Debug.Log($"В слоте 3 находится: {slotData.resource.DisplayName}");
    }
}

// Получить количество слотов
int totalSlots = trunkSlots.SlotCount;
Debug.Log($"Всего слотов: {totalSlots}");
```

## Пошаговая настройка

### 1. Настройка SimpleTrunkOnly
1. Выберите объект машины с компонентом `SimpleTrunkOnly`
2. В инспекторе включите `Manual Slot Configuration`
3. Настройте `Slot Spacing` и `Slot Height` по желанию
4. Нажмите "Setup Trunk Only" в контекстном меню

### 2. Настройка позиций слотов
1. В компоненте `VehicleTrunkSlots` включите `Manual Slot Positions`
2. В `Custom Slot Positions` добавьте нужное количество слотов
3. Для каждого слота настройте:
   - `Slot Name` - уникальное имя
   - `Local Position` - позиция относительно корня багажника
   - `Local Rotation` - поворот слота
   - `Size` - размер коллайдера
   - `Gizmo Color` - цвет в редакторе

### 3. Тестирование
1. Добавьте компонент `TrunkSlotTester` к объекту багажника
2. Настройте `Test Resource` и `Test Prefab`
3. Укажите `Test Slot Index` для тестирования конкретного слота
4. Используйте контекстное меню для тестирования

## Примеры конфигурации

### Сетка 3x4 с ручной настройкой
```csharp
// Слот 0: левый верхний угол
slotName = "Slot_0"
localPosition = new Vector3(-1.0f, 0.1f, 1.5f)

// Слот 1: центр верхнего ряда
slotName = "Slot_1" 
localPosition = new Vector3(0.0f, 0.1f, 1.5f)

// Слот 2: правый верхний угол
slotName = "Slot_2"
localPosition = new Vector3(1.0f, 0.1f, 1.5f)

// И так далее для всех 12 слотов...
```

### Слоты для разных типов ресурсов
```csharp
// Слот для досок (горизонтально)
slotName = "Board_Slot"
localPosition = new Vector3(0.0f, 0.2f, 0.0f)
localRotation = new Vector3(0.0f, 0.0f, 90.0f) // Поворот на 90 градусов
size = new Vector3(2.0f, 0.1f, 0.3f) // Длинный и тонкий

// Слот для ящиков (вертикально)
slotName = "Box_Slot"
localPosition = new Vector3(0.5f, 0.1f, 0.5f)
localRotation = new Vector3(0.0f, 0.0f, 0.0f)
size = new Vector3(0.5f, 0.5f, 0.5f) // Квадратный
```

## Отладка

### Gizmos в редакторе
- Синие кубы показывают позиции слотов
- Зеленые кубы показывают занятые слоты
- Цвета слотов соответствуют настройкам `Gizmo Color`

### Контекстное меню
- `DEBUG/Update Test Visualization` - обновить визуализацию
- `DEBUG/Clear All Slots` - очистить все слоты
- `DEBUG/Print Slot Info` - вывести информацию о слотах
- `DEBUG/Regenerate Slots` - пересоздать слоты

### TrunkSlotTester
- `Test Slot Placement` - протестировать размещение в конкретном слоте
- `Test Random Placement` - протестировать размещение в случайном слоте
- `Clear All Slots` - очистить все слоты
- `Print Slot Info` - вывести информацию о слотах

## Преимущества новой системы

1. **Точность размещения** - ресурсы располагаются именно там, где вы хотите
2. **Реалистичность** - нет "висящих в воздухе" объектов
3. **Гибкость** - каждый слот может иметь свою форму и размер
4. **Визуальность** - четко видно, где находятся слоты в редакторе
5. **Производительность** - физические коллайдеры для взаимодействия
6. **Обратная совместимость** - старые настройки продолжают работать

## Миграция со старой системы

1. Включите `Manual Slot Configuration` в `SimpleTrunkOnly`
2. Включите `Manual Slot Positions` в `VehicleTrunkSlots`
3. Настройте позиции слотов в `Custom Slot Positions`
4. Используйте `Regenerate Slots` для применения изменений

Старые автоматические слоты будут заменены на ручные, но функциональность останется прежней.
