using UnityEngine;

/// Управляет анимацией «несу» и посадкой пропа в руке игрока.
/// Также выставляет флаг CarryMove, когда игрок реально двигается с грузом.
public class PlayerCarryController : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform handSocket;

    [Header("Animator Params")]
    public string carryBoolParam  = "Carry";      // bool: в руках есть предмет
    public string carryTypeParam  = "CarryType";  // int: 0/1/2 (опционально)
    public string carryMoveParam  = "CarryMove";  // bool: несём и ДВИЖЕМСЯ
    public string inputMagParam   = "InputMagnitude"; // если берём скорость из аниматора

    [Header("Speed detection")]
    public bool useAnimatorInputMagnitude = false;   // иначе берём из CC/Rigidbody
    public float moveThreshold = 0.1f;               // порог, чтобы считать, что «движемся»

    public CharacterController characterController;  // (опционально) если есть
    public Rigidbody rigidbodyRef;                   // (опционально) если есть

    [Header("Speed Mul")]
    [Range(0.1f, 2f)] public float baseMoveSpeedMul = 1f;
    public float currentMoveMul { get; private set; } = 1f;

    public GameObject CurrentProp { get; private set; }
    public CarryGrip CurrentGrip { get; private set; }
    public bool IsCarrying => CurrentProp != null;

    // ==== LIFECYCLE ==========================================================

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();
        rigidbodyRef = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!animator) return;

        // Всегда держим Carry в актуальном состоянии (на случай внешних манипуляций)
        if (!string.IsNullOrEmpty(carryBoolParam))
            animator.SetBool(carryBoolParam, IsCarrying);

        // Вычисляем «движемся ли мы сейчас»
        bool moving = false;

        if (useAnimatorInputMagnitude && HasParam(animator, inputMagParam))
        {
            float mag = animator.GetFloat(inputMagParam);
            moving = mag > moveThreshold;
        }
        else
        {
            float horizSpeed = 0f;

            if (characterController)
            {
                Vector3 v = characterController.velocity;
                v.y = 0f;
                horizSpeed = v.magnitude;
            }
            else if (rigidbodyRef)
            {
                Vector3 v = rigidbodyRef.linearVelocity;
                v.y = 0f;
                horizSpeed = v.magnitude;
            }

            moving = horizSpeed > moveThreshold;
        }

        // CarryMove = несем && реально двигаемся
        if (!string.IsNullOrEmpty(carryMoveParam))
            animator.SetBool(carryMoveParam, IsCarrying && moving);
    }

    // ==== PUBLIC API =========================================================

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

        // аниматор: включаем Carry немедленно (CarryMove обновится в Update)
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

        // вернём коллайдеры (RigidBody создашь при укладке/броске при необходимости)
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = true;

        go.transform.SetParent(null, true);
        CurrentProp = null;
        CurrentGrip = null;

        if (animator && !string.IsNullOrEmpty(carryBoolParam))
            animator.SetBool(carryBoolParam, false);

        // CarryMove сбросится автоматически в Update (IsCarrying=false)
        currentMoveMul = baseMoveSpeedMul;
        return go;
    }

    // ==== UTILS ==============================================================
    static bool HasParam(Animator anim, string name)
    {
        if (!anim || string.IsNullOrEmpty(name)) return false;
        foreach (var p in anim.parameters)
            if (p.name == name) return true;
        return false;
    }
}
