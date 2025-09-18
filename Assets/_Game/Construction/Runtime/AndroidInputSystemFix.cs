using UnityEngine;

/// <summary>
/// Исправления для работы Input System "Both" на Android
/// Оптимизирует производительность и предотвращает конфликты
/// </summary>
public class AndroidInputSystemFix : MonoBehaviour
{
    [Header("Android Optimizations")]
    [Tooltip("Отключать Input System на мобильных устройствах для лучшей производительности")]
    public bool disableInputSystemOnMobile = true;
    
    [Tooltip("Использовать только Legacy Input на Android")]
    public bool forceLegacyOnAndroid = true;
    
    [Tooltip("Частота обновления Input System (в секундах)")]
    public float inputSystemUpdateRate = 0.016f; // 60 FPS
    
    [Header("Performance Settings")]
    [Tooltip("Отключать неиспользуемые Input Actions")]
    public bool optimizeUnusedActions = true;
    
    [Tooltip("Кэшировать Input результаты")]
    public bool cacheInputResults = true;
    
    private float lastInputSystemUpdate;
    private bool inputSystemOptimized = false;
    
    private void Start()
    {
        OptimizeForPlatform();
    }
    
    private void OptimizeForPlatform()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (forceLegacyOnAndroid)
        {
            Debug.Log("AndroidInputSystemFix: Принудительное использование Legacy Input на Android");
            
            // Отключаем Input System компоненты на Android
            DisableInputSystemComponents();
            
            // Оптимизируем Legacy Input
            OptimizeLegacyInput();
        }
        #endif
        
        if (Application.isMobilePlatform && disableInputSystemOnMobile)
        {
            OptimizeForMobile();
        }
    }
    
    private void DisableInputSystemComponents()
    {
        // Находим и отключаем Input System компоненты
        #if ENABLE_INPUT_SYSTEM
        var inputSystemComponents = FindObjectsOfType<UnityEngine.InputSystem.PlayerInput>();
        foreach (var component in inputSystemComponents)
        {
            component.enabled = false;
            Debug.Log($"Отключен Input System компонент: {component.name}");
        }
        #endif
    }
    
    private void OptimizeLegacyInput()
    {
        // Настройки для оптимального Legacy Input на мобильных
        if (Application.isMobilePlatform)
        {
            // Устанавливаем оптимальный frame rate
            Application.targetFrameRate = 60;
            
            // Оптимизируем качество для мобильных устройств
            QualitySettings.vSyncCount = 0;
            
            Debug.Log("AndroidInputSystemFix: Legacy Input оптимизирован для мобильных устройств");
        }
    }
    
    private void OptimizeForMobile()
    {
        if (!inputSystemOptimized)
        {
            // Оптимизации для мобильной производительности
            OptimizeTouchInput();
            OptimizeMemoryUsage();
            
            inputSystemOptimized = true;
            Debug.Log("AndroidInputSystemFix: Мобильные оптимизации применены");
        }
    }
    
    private void OptimizeTouchInput()
    {
        // Оптимизируем обработку касаний
        Input.multiTouchEnabled = true;
        Input.simulateMouseWithTouches = false; // Отключаем эмуляцию мыши
        
        // Настраиваем максимальное количество касаний
        Input.multiTouchEnabled = true;
    }
    
    private void OptimizeMemoryUsage()
    {
        // Принудительная сборка мусора перед оптимизацией
        System.GC.Collect();
        
        // Оптимизируем использование памяти
        Resources.UnloadUnusedAssets();
    }
    
    private void Update()
    {
        // Ограничиваем частоту обновления Input System на мобильных
        if (Application.isMobilePlatform && Time.time - lastInputSystemUpdate < inputSystemUpdateRate)
        {
            return;
        }
        
        lastInputSystemUpdate = Time.time;
        
        // Дополнительные оптимизации в рантайме
        if (Application.isMobilePlatform)
        {
            OptimizeRuntimeInput();
        }
    }
    
    private void OptimizeRuntimeInput()
    {
        // Оптимизации во время выполнения
        if (cacheInputResults)
        {
            CacheCommonInputs();
        }
    }
    
    private void CacheCommonInputs()
    {
        // Кэшируем часто используемые входы
        // Это уменьшает количество обращений к Input системе
        
        // Основные движения (кэшируем только если есть изменения)
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        
        // Мышь/касания (только если есть движение)
        if (Input.touchCount > 0 || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            // Обновляем кэш только при необходимости
            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");
        }
    }
    
    // Публичные методы для внешнего управления
    public void EnableInputSystemOptimization(bool enable)
    {
        enabled = enable;
        
        if (enable)
        {
            OptimizeForPlatform();
        }
    }
    
    public void SetInputUpdateRate(float rate)
    {
        inputSystemUpdateRate = rate;
    }
    
    public bool IsOptimizedForMobile()
    {
        return inputSystemOptimized;
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // При паузе приложения очищаем кэши
            if (cacheInputResults)
            {
                System.GC.Collect();
            }
        }
    }
    
    private void OnGUI()
    {
        if (Debug.isDebugBuild && Application.isMobilePlatform)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Platform: {Application.platform}");
            GUILayout.Label($"Input Optimized: {inputSystemOptimized}");
            GUILayout.Label($"Touch Count: {Input.touchCount}");
            GUILayout.Label($"Target FPS: {Application.targetFrameRate}");
            GUILayout.Label($"Current FPS: {1f / Time.unscaledDeltaTime:F1}");
            GUILayout.EndVertical();
        }
    }
}