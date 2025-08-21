using UnityEngine;

/// Версия под мобильную кнопку: показывает Pick/Drop по контексту.
/// Работает с PlayerCarryController и PalletInteractable (slots).
public class PlayerInteractorMobile : MonoBehaviour
{
    [Header("Поиск цели")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public float fallbackRadius = 2.0f;
    public LayerMask interactMask = ~0;

    [Header("Refs")]
    public PlayerCarryController carry;
    public GameObject pickButton;   // “Взять”
    public GameObject dropButton;   // “Положить”

    PalletInteractable _currentTarget;

    void Reset()
    {
        cam = Camera.main;
        carry = GetComponent<PlayerCarryController>();
    }

    void Update()
    {
        _currentTarget = null;
        TryFindTarget(out _currentTarget);

        bool canPick = !carry || !carry.IsCarrying ? _currentTarget != null : false;
        bool canDrop =  carry && carry.IsCarrying  ? _currentTarget != null : false;

        if (pickButton) pickButton.SetActive(canPick);
        if (dropButton) dropButton.SetActive(canDrop);
    }

    // === UI callbacks ===
    public void OnPickButton()
    {
        if (carry == null || carry.IsCarrying || _currentTarget == null) return;

        if (_currentTarget.TryTakeOne(out var prop))
        {
            carry.Attach(prop); // аниматор сам включится
        }
    }

    public void OnDropButton()
    {
        if (carry == null || !carry.IsCarrying || _currentTarget == null) return;

        var held = carry.Detach(); // вернёт объект и выключит аниматор
        if (!held) return;

        // положим в палету; если не получилось — просто бросим рядом
        if (!_currentTarget.TryPutOne(held))
        {
            // запасной дроп на пол
            held.transform.position = transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;
            if (!held.TryGetComponent<Rigidbody>(out var rb)) rb = held.AddComponent<Rigidbody>();
            rb.mass = 5f;
        }
    }

    // === поиск палеты ===
    bool TryFindTarget(out PalletInteractable pallet)
    {
        pallet = null;
        if (!cam) cam = Camera.main;
        if (!cam) return false;

        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(r, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            pallet = hit.collider.GetComponentInParent<PalletInteractable>();
            if (pallet) return true;
        }

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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, fallbackRadius);
    }
#endif
}
