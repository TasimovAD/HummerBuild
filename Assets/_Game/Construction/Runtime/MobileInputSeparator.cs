using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Разделяет касания для движения и камеры на мобильных устройствах
/// Предотвращает конфликт между джойстиком движения и управлением камерой
/// </summary>
public class MobileInputSeparator : MonoBehaviour
{
    [Header("Touch Zone Configuration")]
    [Tooltip("Левая зона для джойстика движения (0.0 - 1.0)")]
    [Range(0f, 0.8f)]
    public float movementZoneEnd = 0.4f;
    
    [Tooltip("Правая зона для управления камерой (0.2 - 1.0)")]
    [Range(0.2f, 1f)]
    public float cameraZoneStart = 0.6f;
    
    [Header("Input Filtering")]
    [Tooltip("Игнорировать касания над UI элементами")]
    public bool ignoreUITouches = true;
    
    [Tooltip("Минимальное расстояние между касаниями для предотвращения конфликтов")]
    public float minTouchSeparation = 50f;
    
    [Header("Debug Visualization")]
    public bool showTouchZones = false;
    public bool logTouchEvents = false;
    
    // Статический доступ для других скриптов
    public static MobileInputSeparator Instance { get; private set; }
    
    // События для подписки
    public System.Action<Touch, TouchZoneType> OnTouchZoneEvent;
    
    public enum TouchZoneType
    {
        Movement,
        Camera,
        Neutral,
        UI
    }
    
    // Трекинг активных касаний
    private System.Collections.Generic.Dictionary<int, TouchData> activeTouches = 
        new System.Collections.Generic.Dictionary<int, TouchData>();
    
    private struct TouchData
    {
        public TouchZoneType zone;
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public bool isMovementTouch;
        public bool isCameraTouch;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        ProcessTouches();
    }
    
    private void ProcessTouches()
    {
        // Обработка всех касаний
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            ProcessTouch(touch);
        }
        
        // Очистка завершенных касаний
        CleanupInactiveTouches();
    }
    
    private void ProcessTouch(Touch touch)
    {
        TouchZoneType zone = GetTouchZone(touch.position);
        
        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(touch, zone);
                break;
                
            case TouchPhase.Moved:
                HandleTouchMoved(touch);
                break;
                
            case TouchPhase.Stationary:
                HandleTouchStationary(touch);
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnded(touch);
                break;
        }
    }
    
    private TouchZoneType GetTouchZone(Vector2 screenPosition)
    {
        // Проверка на UI элементы
        if (ignoreUITouches && IsPointerOverUI(screenPosition))
        {
            return TouchZoneType.UI;
        }
        
        float normalizedX = screenPosition.x / Screen.width;
        
        // Левая зона для движения
        if (normalizedX <= movementZoneEnd)
        {
            return TouchZoneType.Movement;
        }
        // Правая зона для камеры
        else if (normalizedX >= cameraZoneStart)
        {
            return TouchZoneType.Camera;
        }
        // Нейтральная зона посередине
        else
        {
            return TouchZoneType.Neutral;
        }
    }
    
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;
            
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        return results.Count > 0;
    }
    
    private void HandleTouchBegan(Touch touch, TouchZoneType zone)
    {
        // Проверяем конфликт с существующими касаниями
        if (HasConflictingTouch(touch, zone))
        {
            if (logTouchEvents)
                Debug.Log($"Touch {touch.fingerId} ignored due to conflict");
            return;
        }
        
        TouchData touchData = new TouchData
        {
            zone = zone,
            startPosition = touch.position,
            currentPosition = touch.position,
            isMovementTouch = zone == TouchZoneType.Movement,
            isCameraTouch = zone == TouchZoneType.Camera
        };
        
        activeTouches[touch.fingerId] = touchData;
        
        // Уведомляем подписчиков
        OnTouchZoneEvent?.Invoke(touch, zone);
        
        if (logTouchEvents)
        {
            Debug.Log($"Touch {touch.fingerId} began in {zone} zone at {touch.position}");
        }
    }
    
    private void HandleTouchMoved(Touch touch)
    {
        if (!activeTouches.ContainsKey(touch.fingerId))
            return;
            
        TouchData touchData = activeTouches[touch.fingerId];
        touchData.currentPosition = touch.position;
        activeTouches[touch.fingerId] = touchData;
        
        // Проверяем, не ушло ли касание в другую зону
        TouchZoneType currentZone = GetTouchZone(touch.position);
        if (currentZone != touchData.zone)
        {
            // Касание сменило зону - можем игнорировать или обновить
            if (logTouchEvents)
            {
                Debug.Log($"Touch {touch.fingerId} moved from {touchData.zone} to {currentZone}");
            }
        }
    }
    
    private void HandleTouchStationary(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            TouchData touchData = activeTouches[touch.fingerId];
            touchData.currentPosition = touch.position;
            activeTouches[touch.fingerId] = touchData;
        }
    }
    
    private void HandleTouchEnded(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            if (logTouchEvents)
            {
                Debug.Log($"Touch {touch.fingerId} ended");
            }
            
            activeTouches.Remove(touch.fingerId);
        }
    }
    
    private bool HasConflictingTouch(Touch newTouch, TouchZoneType newZone)
    {
        foreach (var kvp in activeTouches)
        {
            TouchData existingTouch = kvp.Value;
            
            // Проверяем расстояние между касаниями
            float distance = Vector2.Distance(newTouch.position, existingTouch.currentPosition);
            
            if (distance < minTouchSeparation)
            {
                return true;
            }
            
            // Проверяем конфликт зон
            if ((newZone == TouchZoneType.Movement && existingTouch.isMovementTouch) ||
                (newZone == TouchZoneType.Camera && existingTouch.isCameraTouch))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void CleanupInactiveTouches()
    {
        var touchesToRemove = new System.Collections.Generic.List<int>();
        
        foreach (var kvp in activeTouches)
        {
            bool touchExists = false;
            
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == kvp.Key)
                {
                    touchExists = true;
                    break;
                }
            }
            
            if (!touchExists)
            {
                touchesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int touchId in touchesToRemove)
        {
            activeTouches.Remove(touchId);
        }
    }
    
    // Публичные методы для проверки состояния касаний
    public bool IsTouchInZone(TouchZoneType zone)
    {
        foreach (var touchData in activeTouches.Values)
        {
            if (touchData.zone == zone)
                return true;
        }
        return false;
    }
    
    public bool HasMovementTouch()
    {
        return IsTouchInZone(TouchZoneType.Movement);
    }
    
    public bool HasCameraTouch()
    {
        return IsTouchInZone(TouchZoneType.Camera);
    }
    
    public int GetActiveTouchCount()
    {
        return activeTouches.Count;
    }
    
    private void OnGUI()
    {
        if (!showTouchZones) return;
        
        // Рисуем зоны касания
        DrawTouchZone(0, 0, Screen.width * movementZoneEnd, Screen.height, 
                     new Color(0, 1, 0, 0.2f), "MOVEMENT");
                     
        DrawTouchZone(Screen.width * cameraZoneStart, 0, 
                     Screen.width * (1f - cameraZoneStart), Screen.height, 
                     new Color(0, 0, 1, 0.2f), "CAMERA");
        
        // Показываем активные касания
        GUILayout.BeginVertical();
        GUILayout.Label($"Active Touches: {activeTouches.Count}");
        
        foreach (var kvp in activeTouches)
        {
            TouchData touch = kvp.Value;
            GUILayout.Label($"Touch {kvp.Key}: {touch.zone} at {touch.currentPosition}");
        }
        
        GUILayout.EndVertical();
    }
    
    private void DrawTouchZone(float x, float y, float width, float height, Color color, string label)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 10, y + 10, 100, 30), label);
    }
}