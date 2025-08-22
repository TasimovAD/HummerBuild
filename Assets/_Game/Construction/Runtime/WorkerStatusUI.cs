// Assets/_Game/Construction/Runtime/WorkerStatusUI.cs
using UnityEngine;
using UnityEngine.UI;
using System;
#if TMP_PRESENT
using TMPro;
#endif

[RequireComponent(typeof(WorkerAgent))]
public class WorkerStatusUI : MonoBehaviour
{
    [Header("Name/State")]
    public string DisplayName = "Worker";
    public float heightOffset = 2.2f;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite buildSprite;
    public Sprite carryFallbackSprite;

    [Header("UI Look")]
    public Vector2 iconSize = new Vector2(38, 38);
    public Color nameColor = Color.white;
    public int nameFontSize = 14;

#if TMP_PRESENT
    public TMP_FontAsset nameTMPFont; // опционально
#else
    public Font nameFont;             // fallback для UGUI Text
#endif

    [Header("Face Player Camera")]
    public Camera playerCamera;              // можно проставить руками
    public Transform lookAtTargetOverride;   // или цель-взгляда (голова игрока)

    [Header("Perf")]
    public float refreshPeriod = 0.2f;

    // runtime
    WorkerAgent _agent;
    Canvas _canvas;
    RectTransform _rtRoot;
    Image _icon;

#if TMP_PRESENT
    TMP_Text _nameTMP;
#else
    Text _nameUGUI;
#endif

    float _nextRefresh;

    void Awake()
    {
        _agent = GetComponent<WorkerAgent>();
        try
        {
            BuildWidget();
        }
        catch (Exception e)
        {
            Debug.LogError($"[WorkerStatusUI] BuildWidget exception on '{name}': {e}", this);
        }

        ResolvePlayerCamera();
        RefreshStatus(); // первичная отрисовка
    }

    void LateUpdate()
    {
        if (!_canvas) return;

        // позиция баннера
        Vector3 head = transform.position + Vector3.up * heightOffset;
        _canvas.transform.position = head;

        // поворот к игроку
        FacePlayer();

        // периодический апдейт
        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + refreshPeriod;
            RefreshStatus();
        }
    }

    void FacePlayer()
    {
        if (lookAtTargetOverride)
        {
            Vector3 dir = lookAtTargetOverride.position - _canvas.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                _canvas.transform.rotation = Quaternion.LookRotation(dir);
            return;
        }

        if (!playerCamera) ResolvePlayerCamera();
        if (playerCamera)
        {
            // смотрим НА камеру
            Vector3 dir = playerCamera.transform.position - _canvas.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                _canvas.transform.rotation = Quaternion.LookRotation(-dir);
        }
    }

    void ResolvePlayerCamera()
    {
        if (playerCamera) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var camInChild = player.GetComponentInChildren<Camera>();
            if (camInChild) { playerCamera = camInChild; return; }
        }

        playerCamera = Camera.main;
        if (!playerCamera)
            Debug.LogWarning("[WorkerStatusUI] Player camera not found (tag=Player or Camera.main). Assign it in inspector.", this);
    }

    void BuildWidget()
    {
        // корневой объект Canvas
        var go = new GameObject("WorkerStatusUI");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 5000;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        go.AddComponent<GraphicRaycaster>();

        _rtRoot = _canvas.GetComponent<RectTransform>();
        _rtRoot.sizeDelta = new Vector2(120, 70);
        _rtRoot.localScale = Vector3.one * 0.01f;

        // отделяем от воркера, но позицию будем выставлять каждый кадр
        go.transform.SetParent(null, false);

        // Иконка
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(_rtRoot, false);
        _icon = iconGO.AddComponent<Image>();
        var rtIcon = _icon.rectTransform;
        rtIcon.sizeDelta = iconSize;
        rtIcon.anchoredPosition = new Vector2(0, -10);

        // Имя
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(_rtRoot, false);

#if TMP_PRESENT
        _nameTMP = nameGO.AddComponent<TMP_Text>();
        if (!_nameTMP)
        {
            Debug.LogWarning("[WorkerStatusUI] TMP_Text not created, falling back to UGUI Text.", this);
            var txt = nameGO.AddComponent<Text>();
            SetupUGUIText(txt);
        }
        else
        {
            _nameTMP.alignment = TextAlignmentOptions.Bottom;
            _nameTMP.color = nameColor;
            _nameTMP.fontSize = nameFontSize;
            _nameTMP.text = DisplayName;
            if (nameTMPFont) _nameTMP.font = nameTMPFont;

            var rtName = _nameTMP.rectTransform;
            rtName.sizeDelta = new Vector2(120, 36);
            rtName.anchoredPosition = new Vector2(0, 20);
        }
#else
        var txt = nameGO.AddComponent<Text>();
        SetupUGUIText(txt);
#endif
    }

#if !TMP_PRESENT
    void SetupUGUIText(Text txt)
    {
        _nameUGUI = txt;
        _nameUGUI.alignment = TextAnchor.LowerCenter;
        _nameUGUI.color = nameColor;
        _nameUGUI.fontSize = nameFontSize;
        _nameUGUI.text = DisplayName;
        if (nameFont) _nameUGUI.font = nameFont;

        var rtName = _nameUGUI.rectTransform;
        rtName.sizeDelta = new Vector2(120, 36);
        rtName.anchoredPosition = new Vector2(0, 20);
    }
#endif

    void RefreshStatus()
    {
        // имя
#if TMP_PRESENT
        if (_nameTMP) _nameTMP.text = DisplayName;
        else if (_nameUGUI) _nameUGUI.text = DisplayName;
#else
        if (_nameUGUI) _nameUGUI.text = DisplayName;
#endif

        // 1) несёт?
        if (_agent != null && _agent.IsCarrying)
        {
            Sprite s = null;
            var res = _agent.CurrentCarryResource;
            if (res && res.Icon) s = res.Icon;
            if (!s) s = carryFallbackSprite;
            SetIcon(s);
            return;
        }

        // 2) строит?
        if (IsLikelyBuilding())
        {
            SetIcon(buildSprite);
            return;
        }

        // 3) idle
        SetIcon(idleSprite);
    }

    bool IsLikelyBuilding()
    {
        if (_agent == null || _agent.Agent == null) return false;

        const float near = 2f;
        var allSites = FindObjectsOfType<BuildSite>();
        foreach (var s in allSites)
        {
            if (!s) continue;
            if ((s.transform.position - transform.position).sqrMagnitude <= near * near)
                return true;
        }
        return false;
    }

    void SetIcon(Sprite s)
    {
        if (_icon) _icon.sprite = s;
    }

    // public helpers
    public void SetName(string newName)
    {
        DisplayName = newName;
#if TMP_PRESENT
        if (_nameTMP) _nameTMP.text = DisplayName;
        else if (_nameUGUI) _nameUGUI.text = DisplayName;
#else
        if (_nameUGUI) _nameUGUI.text = DisplayName;
#endif
    }

    public void SetPlayerCamera(Camera cam) => playerCamera = cam;
    public void SetLookAtTarget(Transform t) => lookAtTargetOverride = t;
}
