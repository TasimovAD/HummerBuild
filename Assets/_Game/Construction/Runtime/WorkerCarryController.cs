using UnityEngine;
#if USING_ANIMATION_RIGGING
using UnityEngine.Animations.Rigging;
#endif

/// Управляет анимацией переноски и позиционированием пропа у рабочего.
public class WorkerCarryController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;
    public Transform handSocket;  // совпадает с HandCarrySocket у рабочего

#if USING_ANIMATION_RIGGING
    [Header("Animation Rigging (опц.)")]
    public TwoBoneIKConstraint leftHandIK;
    public TwoBoneIKConstraint rightHandIK;
#endif

    [Header("Параметры аниматора")]
    public string carryBoolParam = "Carry";
    public string carryTypeParam = "CarryType";

    [Header("Скорости")]
    [Range(0.1f, 2f)] public float baseMoveSpeedMul = 1f;
    public float currentMoveMul { get; private set; } = 1f;

    public GameObject CurrentProp { get; private set; }
    public CarryGrip CurrentGrip { get; private set; }
    public bool IsCarrying => CurrentProp != null;

    public void Attach(GameObject prop)
    {
        Detach();
        if (!prop || !handSocket) return;

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

        if (prop.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
        foreach (var col in prop.GetComponentsInChildren<Collider>()) col.enabled = false;

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

#if USING_ANIMATION_RIGGING
        ApplyIKTargets(true);
#endif

        // <<< ключевая строка: берём коэффициент для РАБОЧЕГО
        currentMoveMul = CurrentGrip ? CurrentGrip.workerSpeedMul : baseMoveSpeedMul;
    }

    public void Detach()
    {
#if USING_ANIMATION_RIGGING
        ApplyIKTargets(false);
#endif
        if (animator && !string.IsNullOrEmpty(carryBoolParam))
            animator.SetBool(carryBoolParam, false);

        if (CurrentProp)
        {
            // ВАЖНО: проверяем, что объект все еще существует и прикреплен
            if (CurrentProp.transform.parent == handSocket)
            {
                foreach (var c in CurrentProp.GetComponentsInChildren<Collider>()) c.enabled = true;
                CurrentProp.transform.SetParent(null);
            }
        }

        CurrentProp = null;
        CurrentGrip = null;
        currentMoveMul = baseMoveSpeedMul;
    }

#if USING_ANIMATION_RIGGING
    void ApplyIKTargets(bool on)
    {
        if (!on)
        {
            if (leftHandIK)  leftHandIK.weight = 0f;
            if (rightHandIK) rightHandIK.weight = 0f;
            return;
        }

        if (!CurrentGrip) return;

        if (leftHandIK && CurrentGrip.leftHandIKTarget)
        {
            leftHandIK.data.target = CurrentGrip.leftHandIKTarget;
            leftHandIK.weight = 1f;
        }
        if (rightHandIK && CurrentGrip.rightHandIKTarget)
        {
            rightHandIK.data.target = CurrentGrip.rightHandIKTarget;
            rightHandIK.weight = 1f;
        }
    }
#endif
}
