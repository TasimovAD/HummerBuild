using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
using StarterAssets;
#endif

/// <summary>
/// Система входа/выхода в машину для Unity 6 Starter Assets - ThirdPerson + RCCP
/// Адаптирован под новую систему ввода Unity Input System
/// </summary>
public class CarEnterExit_Unity6StarterAssets : MonoBehaviour
{
    [Header("RCCP Car Components")]
    [Tooltip("Компонент управления машиной RCCP_CarController")]
    public RCCP_CarController carController;

    [Tooltip("Корень RCCP-камеры (объект с настройками камеры)")]
    public GameObject rccpCameraRoot;

    [Header("Unity 6 Starter Assets Player Components")]
    [Tooltip("Корневой объект персонажа (с компонентами Starter Assets)")]
    public GameObject playerRoot;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [Tooltip("Компонент ThirdPersonController (движение персонажа)")]
    public ThirdPersonController thirdPersonController;

    [Tooltip("Компонент StarterAssetsInputs (управление вводом)")]
    public StarterAssetsInputs starterAssetsInputs;

    [Tooltip("Компонент PlayerInput (Unity Input System)")]
    public PlayerInput playerInput;
#endif

    [Tooltip("Корень камеры персонажа (Cinemachine FreeLookCamera)")]
    public GameObject playerCameraRoot;

    [Tooltip("Рендереры тела/одежды персонажа")]
    public Renderer[] playerRenderers;

    [Header("UI Elements")]
    [Tooltip("Кнопка входа в машину")]
    public Button enterButton;

    [Tooltip("Кнопка выхода из машины")]
    public Button exitButton;

    [Tooltip("UI машины (HUD, спидометр)")]
    public GameObject carUIRoot;

    [Tooltip("UI персонажа")]
    public GameObject playerUIRoot;

    [Header("Transform Points")]
    [Tooltip("Точка посадки в машину (сиденье водителя)")]
    public Transform seatPoint;

    [Tooltip("Точка выхода из машины")]
    public Transform exitPoint;

    [Header("Settings")]
    [Tooltip("Отключать Cursor Lock при входе в машину")]
    public bool disableCursorLockInCar = true;

    [Tooltip("Название Action Map для машины в Input Actions")]
    public string carActionMapName = "Vehicle";

    [Tooltip("Название Action Map для персонажа в Input Actions")]
    public string playerActionMapName = "Player";

    // Приватные переменные
    private bool _inCar = false;
    private Collider[] _playerColliders;
    private CharacterController _characterController;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    private bool _wasPlayerCursorLocked = true;
    private CursorLockMode _originalCursorLockMode;
    private bool _originalCursorInputForLook;
#endif

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        SetupInitialState();
    }

    private void OnEnable()
    {
        if (enterButton) enterButton.onClick.AddListener(EnterCar);
        if (exitButton) exitButton.onClick.AddListener(ExitCar);
    }

    private void OnDisable()
    {
        if (enterButton) enterButton.onClick.RemoveListener(EnterCar);
        if (exitButton) exitButton.onClick.RemoveListener(ExitCar);
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        // Получаем компоненты автоматически, если не заданы
        if (!playerRoot)
            playerRoot = GameObject.FindGameObjectWithTag("Player");

        if (playerRoot)
        {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            if (!thirdPersonController)
                thirdPersonController = playerRoot.GetComponent<ThirdPersonController>();
            
            if (!starterAssetsInputs)
                starterAssetsInputs = playerRoot.GetComponent<StarterAssetsInputs>();
            
            if (!playerInput)
                playerInput = playerRoot.GetComponent<PlayerInput>();
#endif

            if (!_characterController)
                _characterController = playerRoot.GetComponent<CharacterController>();

            // Получаем все коллайдеры персонажа
            _playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);
        }

        // Поиск камеры персонажа
        if (!playerCameraRoot)
        {
            var cinemachineCamera = FindObjectOfType<Cinemachine.CinemachineFreeLook>();
            if (cinemachineCamera)
                playerCameraRoot = cinemachineCamera.gameObject;
        }
    }

    private void SetupInitialState()
    {
        // Начальное состояние: персонаж активен, машина неактивна
        SafeSetCarActive(false);
        EnsureRccpCameraRootActive();
        SetRccpCameraRender(false);

        // UI состояние
        if (carUIRoot) carUIRoot.SetActive(false);
        if (playerUIRoot) playerUIRoot.SetActive(true);
        if (enterButton) enterButton.gameObject.SetActive(false);
        if (exitButton) exitButton.gameObject.SetActive(false);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Показать/скрыть кнопку входа в машину (вызывается триггером)
    /// </summary>
    public void ShowEnterButton(bool show)
    {
        if (_inCar) return;
        if (enterButton) enterButton.gameObject.SetActive(show);
    }

    /// <summary>
    /// Вход в машину
    /// </summary>
    public void EnterCar()
    {
        if (_inCar) return;
        _inCar = true;

        Debug.Log("Entering car...");

        // 1. Отключаем управление персонажем
        SetPlayerControlEnabled(false);

        // 2. Отключаем камеру персонажа
        if (playerCameraRoot) playerCameraRoot.SetActive(false);

        // 3. Скрываем UI персонажа
        if (playerUIRoot) playerUIRoot.SetActive(false);

        // 4. Скрываем визуал и коллайдеры персонажа
        SetPlayerVisible(false);
        SetPlayerColliders(false);

        // 5. Перемещаем персонажа на сиденье
        if (seatPoint && playerRoot)
        {
            playerRoot.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
        }

        // 6. Переключаем Input Actions на машину
        SwitchToCarInputs();

        // 7. Включаем камеру машины
        SetRccpCameraRender(true);

        // 8. Включаем управление машиной
        SafeSetCarActive(true);

        // 9. Показываем UI машины и переключаем кнопки
        if (carUIRoot) carUIRoot.SetActive(true);
        if (enterButton) enterButton.gameObject.SetActive(false);
        if (exitButton) exitButton.gameObject.SetActive(true);

        // 10. Управление курсором для машины
        HandleCursorForCar();
    }

    /// <summary>
    /// Выход из машины
    /// </summary>
    public void ExitCar()
    {
        if (!_inCar) return;
        _inCar = false;

        Debug.Log("Exiting car...");

        // 1. Отключаем управление машиной
        SafeSetCarActive(false);

        // 2. Отключаем камеру машины
        SetRccpCameraRender(false);

        // 3. Скрываем UI машины
        if (carUIRoot) carUIRoot.SetActive(false);

        // 4. Перемещаем персонажа на точку выхода
        if (exitPoint && playerRoot)
        {
            playerRoot.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);
        }

        // 5. Возвращаем коллайдеры и видимость персонажа
        SetPlayerColliders(true);
        SetPlayerVisible(true);

        // 6. Переключаем Input Actions обратно на персонажа
        SwitchToPlayerInputs();

        // 7. Включаем управление персонажем
        SetPlayerControlEnabled(true);

        // 8. Включаем камеру персонажа
        if (playerCameraRoot) playerCameraRoot.SetActive(true);

        // 9. Возвращаем UI персонажа
        if (playerUIRoot) playerUIRoot.SetActive(true);

        // 10. Переключаем кнопки
        if (exitButton) exitButton.gameObject.SetActive(false);

        // 11. Восстанавливаем настройки курсора персонажа
        RestoreCursorForPlayer();
    }

    #endregion

    #region Private Helper Methods

    private void SetPlayerControlEnabled(bool enabled)
    {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        // Отключаем/включаем компоненты Starter Assets
        if (thirdPersonController)
            thirdPersonController.enabled = enabled;

        if (starterAssetsInputs)
            starterAssetsInputs.enabled = enabled;
#endif

        // Отключаем CharacterController чтобы персонаж не падал
        if (_characterController)
            _characterController.enabled = enabled;
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers != null)
        {
            foreach (var renderer in playerRenderers)
            {
                if (renderer) renderer.enabled = visible;
            }
        }
    }

    private void SetPlayerColliders(bool enabled)
    {
        if (_playerColliders != null)
        {
            foreach (var collider in _playerColliders)
            {
                if (collider && collider != _characterController)
                    collider.enabled = enabled;
            }
        }
    }

    private void SafeSetCarActive(bool enabled)
    {
        if (carController) 
            carController.enabled = enabled;
    }

    private void EnsureRccpCameraRootActive()
    {
        if (rccpCameraRoot && !rccpCameraRoot.activeSelf)
            rccpCameraRoot.SetActive(true);
    }

    private void SetRccpCameraRender(bool render)
    {
        if (!rccpCameraRoot) return;

        EnsureRccpCameraRootActive();

        var camera = rccpCameraRoot.GetComponentInChildren<Camera>(true);
        if (camera) camera.enabled = render;

        var audioListener = rccpCameraRoot.GetComponentInChildren<AudioListener>(true);
        if (audioListener) audioListener.enabled = render;
    }

    private void SwitchToCarInputs()
    {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        if (!playerInput) return;

        try
        {
            // Отключаем Action Map персонажа
            var playerActionMap = playerInput.actions.FindActionMap(playerActionMapName);
            if (playerActionMap != null)
                playerActionMap.Disable();

            // Включаем Action Map машины (если есть)
            var carActionMap = playerInput.actions.FindActionMap(carActionMapName);
            if (carActionMap != null)
                carActionMap.Enable();

            Debug.Log($"Switched to car inputs: {carActionMapName}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to switch to car inputs: {e.Message}");
        }
#endif
    }

    private void SwitchToPlayerInputs()
    {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        if (!playerInput) return;

        try
        {
            // Отключаем Action Map машины
            var carActionMap = playerInput.actions.FindActionMap(carActionMapName);
            if (carActionMap != null)
                carActionMap.Disable();

            // Включаем Action Map персонажа
            var playerActionMap = playerInput.actions.FindActionMap(playerActionMapName);
            if (playerActionMap != null)
                playerActionMap.Enable();

            Debug.Log($"Switched to player inputs: {playerActionMapName}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to switch to player inputs: {e.Message}");
        }
#endif
    }

    private void HandleCursorForCar()
    {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        if (!disableCursorLockInCar) return;

        // Сохраняем текущие настройки курсора
        _originalCursorLockMode = Cursor.lockState;
        _wasPlayerCursorLocked = Cursor.lockState == CursorLockMode.Locked;

        if (starterAssetsInputs)
            _originalCursorInputForLook = starterAssetsInputs.cursorInputForLook;

        // Освобождаем курсор для управления машиной
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (starterAssetsInputs)
            starterAssetsInputs.cursorInputForLook = false;
#endif
    }

    private void RestoreCursorForPlayer()
    {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        if (!disableCursorLockInCar) return;

        // Восстанавливаем настройки курсора
        Cursor.lockState = _originalCursorLockMode;
        Cursor.visible = !_wasPlayerCursorLocked;

        if (starterAssetsInputs)
            starterAssetsInputs.cursorInputForLook = _originalCursorInputForLook;
#endif
    }

    #endregion

    #region Auto Setup (Editor Helper)

    [ContextMenu("Auto Assign Components")]
    private void AutoAssignComponents()
    {
        InitializeComponents();

        // Попытка найти рендереры персонажа
        if (playerRoot && (playerRenderers == null || playerRenderers.Length == 0))
        {
            var renderers = new System.Collections.Generic.List<Renderer>();
            renderers.AddRange(playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            renderers.AddRange(playerRoot.GetComponentsInChildren<MeshRenderer>(true));
            playerRenderers = renderers.ToArray();
        }

        Debug.Log("Auto assignment completed. Please check assigned components.");
    }

    #endregion

    #region Public Properties

    public bool IsInCar => _inCar;

    #endregion
}