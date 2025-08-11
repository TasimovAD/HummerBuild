using UnityEngine;
using UnityEngine.UI;
// ВАЖНО: если у тебя класс RCCP_CarController лежит в пространстве имён (например, RCCP),
// добавь здесь using RCCP;  ИЛИ укажи полный путь RCCP.RCCP_CarController
// using RCCP;

public class CarEnterExit_RCCP : MonoBehaviour
{
    [Header("RCCP")]
    [Tooltip("Компонент управления машиной RCCP_CarController на этой машине")]
    public RCCP_CarController carController; // если не видит тип — добавь using с нужным неймспейсом

    [Tooltip("Корень RCCP-камеры (объект с настройками камеры, pivot и т.д.)")]
    public GameObject rccpCameraRoot;

    [Header("Player / Invector")]
    [Tooltip("Корневой объект персонажа (родитель всех компонентов Invector)")]
    public GameObject playerRoot;

    [Tooltip("Компоненты управления персонажем (vThirdPersonController, vThirdPersonInput и т.п.)")]
    public Behaviour[] playerControlComponents;

    [Tooltip("Корень камеры персонажа (обычно объект vThirdPersonCamera/ Invector Camera)")]
    public GameObject playerCameraRoot;

    [Tooltip("Рендереры тела/одежды персонажа, чтобы скрывать модель в машине")]
    public Renderer[] playerRenderers;

    [Header("UI")]
    [Tooltip("Кнопка входа в машину (показывается по триггеру)")]
    public Button enterButton;

    [Tooltip("Кнопка выхода из машины (активна, только когда мы в машине)")]
    public Button exitButton;

    [Tooltip("UI машины (HUD, спидометр и т.д.)")]
    public GameObject carUIRoot;

    [Tooltip("UI персонажа (кнопки ходьбы/инвентарь и т.д.)")]
    public GameObject playerUIRoot;

    [Header("Точки посадки/выхода")]
    [Tooltip("Куда посадить персонажа при входе (сиденье водителя)")]
    public Transform seatPoint;

    [Tooltip("Куда поставить персонажа при выходе")]
    public Transform exitPoint;

    private bool _inCar = false;
    private Collider[] _playerColliders;

    private void Awake()
    {
        // Старт: персонаж активен, машина неуправляема, камера RCCP "жива", но не рендерит
        SafeSetCarActive(false);

        // Делаем корень RCCP-камеры активным (чтобы корутины могли стартовать), но рендер выключен
        EnsureRccpCameraRootActive();
        SetRccpCameraRender(false);

        if (carUIRoot) carUIRoot.SetActive(false);
        if (playerUIRoot) playerUIRoot.SetActive(true);

        if (enterButton) enterButton.gameObject.SetActive(false);
        if (exitButton)  exitButton.gameObject.SetActive(false);

        if (playerRoot)
            _playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        if (enterButton) enterButton.onClick.AddListener(EnterCar);
        if (exitButton)  exitButton.onClick.AddListener(ExitCar);
    }

    private void OnDisable()
    {
        if (enterButton) enterButton.onClick.RemoveListener(EnterCar);
        if (exitButton)  exitButton.onClick.RemoveListener(ExitCar);
    }

    // Вызывается триггером у двери
    public void ShowEnterButton(bool show)
    {
        if (_inCar) return;
        if (enterButton) enterButton.gameObject.SetActive(show);
    }

    public void EnterCar()
    {
        if (_inCar) return;
        _inCar = true;

        // 1) Отключаем управление, камеру и UI персонажа
        SetPlayerControl(false);
        if (playerCameraRoot) playerCameraRoot.SetActive(false);
        if (playerUIRoot) playerUIRoot.SetActive(false);

        // 2) Скрываем визуал и коллайдеры персонажа, чтобы не мешал в салоне
        SetPlayerVisible(false);
        SetPlayerColliders(false);

        // 3) Перемещаем персонажа на сиденье (без анимаций)
        if (seatPoint && playerRoot)
            playerRoot.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);

        // 4) Подготовка камеры RCCP:
        //    Корень камеры уже активен (Awake), просто включаем её рендер
        SetRccpCameraRender(true);

        // 5) Включаем машину (после того, как камера "живая") — чтобы не падали корутины RCCP
        SafeSetCarActive(true);

        // 6) Включаем HUD машины, переключаем кнопки
        if (carUIRoot) carUIRoot.SetActive(true);
        if (enterButton) enterButton.gameObject.SetActive(false);
        if (exitButton)  exitButton.gameObject.SetActive(true);
    }

    public void ExitCar()
    {
        if (!_inCar) return;
        _inCar = false;

        // 1) Сначала выключаем управление машиной
        SafeSetCarActive(false);

        // 2) Гасим рендер RCCP-камеры (корень остаётся активным)
        SetRccpCameraRender(false);

        // 3) Выключаем HUD машины
        if (carUIRoot) carUIRoot.SetActive(false);

        // 4) Ставим персонажа на точку выхода и возвращаем управление/видимость/коллайдеры
        if (exitPoint && playerRoot)
            playerRoot.transform.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);

        SetPlayerColliders(true);
        SetPlayerVisible(true);
        SetPlayerControl(true);

        // 5) Возвращаем камеру и UI персонажа
        if (playerCameraRoot) playerCameraRoot.SetActive(true);
        if (playerUIRoot) playerUIRoot.SetActive(true);

        // 6) Кнопки
        if (exitButton)  exitButton.gameObject.SetActive(false);
        // Enter появится снова по триггеру
    }

    // ===== Вспомогательные методы =====

    private void SetPlayerControl(bool enable)
    {
        if (playerControlComponents != null)
        {
            foreach (var comp in playerControlComponents)
                if (comp) comp.enabled = enable;
        }
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers != null)
        {
            foreach (var r in playerRenderers)
                if (r) r.enabled = visible;
        }
        // Альтернатива: playerRoot.SetActive(visible) — но следи за логикой ссылок/UI.
    }

    private void SetPlayerColliders(bool enable)
    {
        if (_playerColliders != null)
        {
            foreach (var c in _playerColliders)
                if (c) c.enabled = enable;
        }
    }

    private void SafeSetCarActive(bool enable)
    {
        if (carController) carController.enabled = enable;
    }

    private void EnsureRccpCameraRootActive()
    {
        if (rccpCameraRoot && !rccpCameraRoot.activeSelf)
            rccpCameraRoot.SetActive(true); // корень камеры всегда активен -> корутины RCCP не падают
    }

    /// <summary>
    /// Включает/выключает РЕНДЕР RCCP-камеры (Camera/AudioListener), не трогая активность объекта.
    /// Так RCCP_Camera остаётся активной и может запускать корутины.
    /// </summary>
    private void SetRccpCameraRender(bool render)
    {
        if (!rccpCameraRoot) return;

        // На всякий случай убедимся, что корень активен
        EnsureRccpCameraRootActive();

        var cam = rccpCameraRoot.GetComponentInChildren<Camera>(true);
        if (cam) cam.enabled = render;

        var al = rccpCameraRoot.GetComponentInChildren<AudioListener>(true);
        if (al) al.enabled = render;

        // Если хочешь дополнительно гасить сам скрипт RCCP_Camera (необязательно):
        // var rccpCam = rccpCameraRoot.GetComponentInChildren<MonoBehaviour>(true);
        // if (rccpCam && rccpCam.GetType().Name == "RCCP_Camera") rccpCam.enabled = render;
    }

    // ——— Удобная автозаправка полей (по желанию) ———
    [ContextMenu("Auto Assign Player Stuff")]
    private void AutoAssign()
    {
        if (!playerRoot) return;

        // Камера персонажа — если не задана, попробуем найти по типу/имени
        if (!playerCameraRoot)
        {
            // Попытайся найти объект с компонентом vThirdPersonCamera (если тип доступен)
            // var invCam = FindObjectOfType<vThirdPersonCamera>();
            // if (invCam) playerCameraRoot = invCam.gameObject;

            // Или хотя бы по имени
            var maybeCam = GameObject.Find("vThirdPersonCamera");
            if (!maybeCam) maybeCam = GameObject.Find("InvectorCamera");
            if (maybeCam) playerCameraRoot = maybeCam;
        }

        // Контроллеры персонажа
        if (playerControlComponents == null || playerControlComponents.Length == 0)
        {
            var list = new System.Collections.Generic.List<Behaviour>();
            // var c1 = playerRoot.GetComponentInChildren<vThirdPersonController>(true);
            // var c2 = playerRoot.GetComponentInChildren<vThirdPersonInput>(true);
            // if (c1) list.Add(c1);
            // if (c2) list.Add(c2);
            playerControlComponents = list.ToArray();
        }

        // Рендереры персонажа
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            var skinned = playerRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mesh    = playerRoot.GetComponentsInChildren<MeshRenderer>(true);
            var list = new System.Collections.Generic.List<Renderer>();
            list.AddRange(skinned);
            list.AddRange(mesh);
            playerRenderers = list.ToArray();
        }
    }
}
