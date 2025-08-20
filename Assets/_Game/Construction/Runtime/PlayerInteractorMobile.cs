using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractorMobile : MonoBehaviour
{
    [Header("Поиск цели")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public float fallbackRadius = 2.0f;
    public LayerMask interactMask = ~0;

    [Header("Ссылки")]
    public PlayerCarry carry;
    public GameObject pickButton;   // кнопка “Взять”
    public GameObject dropButton;   // кнопка “Положить”

    PalletInteractable _currentTarget;

    void Reset()
    {
        cam = Camera.main;
        carry = GetComponent<PlayerCarry>();
    }

    void Update()
    {
        // Поиск цели (каждый кадр)
        _currentTarget = null;
        TryFindTarget(out _currentTarget);

        // Управляем кнопками
        if (pickButton) pickButton.SetActive(carry.IsEmpty && _currentTarget != null);
        if (dropButton) dropButton.SetActive(!carry.IsEmpty && _currentTarget != null);
    }

    // Кнопка “Взять”
    public void OnPickButton()
    {
        if (!carry || !carry.IsEmpty || _currentTarget == null) return;

        if (_currentTarget.TryTakeOne(out var res))
        {
            carry.Pickup(res);
        }
    }

    // Кнопка “Положить”
    public void OnDropButton()
    {
        if (!carry || carry.IsEmpty || _currentTarget == null) return;

        if (_currentTarget.TryPutOne(carry.CarriedRes))
        {
            carry.ClearHand();
        }
    }

    // --- Поиск цели ---
    bool TryFindTarget(out PalletInteractable pallet)
    {
        pallet = null;
        if (!cam)
        {
            cam = Camera.main;
            if (!cam) return false;
        }

        // 1) Луч из центра экрана
        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(r, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            pallet = hit.collider.GetComponentInParent<PalletInteractable>();
            if (pallet != null) return true;
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
            if (d < best && d <= interactDistance)
            {
                best = d;
                bestPal = p;
            }
        }

        if (bestPal != null)
        {
            pallet = bestPal;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, fallbackRadius);
    }
#endif
}
