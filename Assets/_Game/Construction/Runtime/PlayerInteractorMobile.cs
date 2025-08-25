using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Версия под мобильные кнопки: Pick/Drop + сообщение об ошибке
public class PlayerInteractorMobile : MonoBehaviour
{
    [Header("Поиск цели")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public float fallbackRadius = 2.0f;
    public LayerMask interactMask = ~0;

    [Header("Refs")]
    public PlayerCarryController carry; // контроллер переноски игрока
    public GameObject pickButton;       // “Взять”
    public GameObject dropButton;       // “Положить”

    [Header("UI Сообщения")]
    public TMP_Text errorText;          // куда выводим “Тут хранится другой ресурс”
    public float errorShowSeconds = 1.5f;

    PalletInteractable _currentTarget;
    float _errorHideAt = -1f;

    void Reset()
    {
        cam = Camera.main;
        carry = GetComponent<PlayerCarryController>();
    }

    void Update()
    {
        // авто-скрытие сообщения
        if (errorText && _errorHideAt > 0 && Time.unscaledTime >= _errorHideAt)
        {
            errorText.gameObject.SetActive(false);
            _errorHideAt = -1f;
        }

        // ищем ближайшую палету
        _currentTarget = null;
        TryFindTarget(out _currentTarget);

        bool canPick = (!carry || !carry.IsCarrying) && _currentTarget != null;
        bool canDrop = (carry && carry.IsCarrying) && _currentTarget != null;

        if (pickButton) pickButton.SetActive(canPick);
        if (dropButton) dropButton.SetActive(canDrop);
    }

    // ==== Кнопки ====

    public void OnPickButton()
    {
        if (carry == null || carry.IsCarrying || _currentTarget == null) return;

        if (_currentTarget.TryTakeOne(out var prop))
        {
            carry.Attach(prop); // аниматор включится сам
        }
    }

    public void OnDropButton()
    {
        if (carry == null || !carry.IsCarrying || _currentTarget == null) return;

        // ВАЖНО: не отцепляем заранее! Берём ссылку на объект в руках
        var held = carry.CurrentProp;
        if (!held) return;

        // Пытаемся положить в палету напрямую
        bool ok = _currentTarget.TryPutOne(held);

        if (ok)
        {
            // Только при успехе отцепляем из рук (иконки/аниматор выключатся внутри Detach)
            carry.Detach();
        }
        else
        {
            // Палета не подходит (другой ресурс / нет слотов) — оставляем в руках и покажем текст
            ShowError("Тут хранится другой ресурс");
        }
    }

    // ==== Поиск палеты ====
    bool TryFindTarget(out PalletInteractable pallet)
    {
        pallet = null;
        if (!cam) cam = Camera.main;
        if (!cam) return false;

        // 1) Лучом из центра экрана
        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(r, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            pallet = hit.collider.GetComponentInParent<PalletInteractable>();
            if (pallet) return true;
        }

        // 2) Поиск ближайшего в радиусе
        var cols = Physics.OverlapSphere(transform.position, fallbackRadius, interactMask, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        PalletInteractable bestPal = null;

        foreach (var c in cols)
        {
            var p = c.GetComponentInParent<PalletInteractable>();
            if (!p) continue;

            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best && d <= interactDistance) { best = d; bestPal = p; }
        }

        pallet = bestPal;
        return pallet != null;
    }

    // ==== Сообщение об ошибке ====
    void ShowError(string msg)
    {
        if (!errorText) return;
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
        _errorHideAt = Time.unscaledTime + errorShowSeconds;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, fallbackRadius);
    }
#endif
}
