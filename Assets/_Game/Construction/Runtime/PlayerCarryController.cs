using UnityEngine;

/// Управляет анимацией «несу» и посадкой пропа в руке игрока.
public class PlayerCarryController : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform handSocket;

    [Header("Animator Params")]
    public string carryBoolParam = "Carry";   // bool в контроллере игрока
    public string carryTypeParam = "CarryType"; // опц. int: 0/1/2 если используешь

    [Header("Speed Mul")]
    [Range(0.1f, 2f)] public float baseMoveSpeedMul = 1f;
    public float currentMoveMul { get; private set; } = 1f;

    public GameObject CurrentProp { get; private set; }
    public CarryGrip CurrentGrip { get; private set; }
    public bool IsCarrying => CurrentProp != null;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    /// Прикрепить уже созданный объект (например, взятый со слота)
    public bool Attach(GameObject prop)
    {
        Detach(); // на всякий случай

        if (!prop || !handSocket) return false;

        CurrentProp = prop;
        CurrentGrip = prop.GetComponentInChildren<CarryGrip>();

        prop.transform.SetParent(handSocket, false);

        if (CurrentGrip)
        {
            prop.transform.localPosition = CurrentGrip.localPosition;
            prop.transform.localRotation = Quaternion.Euler(CurrentGrip.localEulerAngles);
            prop.transform.localScale    = CurrentGrip.localScale;
        }
        else
        {
            prop.transform.localPosition = Vector3.zero;
            prop.transform.localRotation = Quaternion.identity;
        }

        // выключим физику, чтобы не мешала руке
        if (prop.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
        foreach (var col in prop.GetComponentsInChildren<Collider>()) col.enabled = false;

        // аниматор
        if (animator && !string.IsNullOrEmpty(carryBoolParam))
            animator.SetBool(carryBoolParam, true);

        if (animator && !string.IsNullOrEmpty(carryTypeParam))
        {
            int type = 0;
            if (CurrentGrip)
            {
                switch (CurrentGrip.style)
                {
                    case CarryStyle.OneHand:      type = 0; break;
                    case CarryStyle.TwoHandFront: type = 1; break;
                    case CarryStyle.Shoulder:     type = 2; break;
                }
            }
            animator.SetInteger(carryTypeParam, type);
        }

        currentMoveMul = CurrentGrip ? CurrentGrip.moveSpeedMul : baseMoveSpeedMul;
        return true;
    }

    /// Отцепить из руки (без уничтожения). Возвращает объект.
    public GameObject Detach()
    {
        if (!CurrentProp) return null;

        var go = CurrentProp;
        // включим коллайдеры обратно (Rigidbody создаёт уже при укладке, если нужно)
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = true;

        go.transform.SetParent(null, true);
        CurrentProp = null;
        CurrentGrip = null;

        if (animator && !string.IsNullOrEmpty(carryBoolParam))
            animator.SetBool(carryBoolParam, false);

        currentMoveMul = baseMoveSpeedMul;
        return go;
    }
}
