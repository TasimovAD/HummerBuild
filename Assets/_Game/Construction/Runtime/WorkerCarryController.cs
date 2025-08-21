using UnityEngine;
#if USING_ANIMATION_RIGGING
using UnityEngine.Animations.Rigging;
#endif

/// Управляет анимацией переноски и позиционированием пропа у рабочего.
public class WorkerCarryController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;
    public Transform handSocket;          // куда крепим проп (совпадает с HandCarrySocket в твоём агенте)

#if USING_ANIMATION_RIGGING
    [Header("Animation Rigging (опц.)")]
    public TwoBoneIKConstraint leftHandIK;
    public TwoBoneIKConstraint rightHandIK;
#endif

    [Header("Параметры аниматора")]
    public string carryBoolParam = "Carry";      // bool: несём/не несём
    public string carryTypeParam = "CarryType";  // int: 0=OneHand, 1=TwoHandFront, 2=Shoulder

    [Header("Скорости")]
    [Range(0.1f, 2f)] public float baseMoveSpeedMul = 1f; // множитель по умолчанию (1)
    public float currentMoveMul = 1f; // на чтение извне (например, для NavMeshAgent.speed)

    public GameObject CurrentProp { get; private set; }
    public CarryGrip CurrentGrip { get; private set; }

    public bool IsCarrying => CurrentProp != null;

    public void Attach(GameObject prop)
    {
        Detach(); // на всякий случай

        if (!prop || !handSocket) return;

        CurrentProp = prop;
        CurrentGrip = prop.GetComponentInChildren<CarryGrip>();

        // parent under socket
        prop.transform.SetParent(handSocket, worldPositionStays: false);

        if (CurrentGrip)
        {
            prop.transform.localPosition = CurrentGrip.localPosition;
            prop.transform.localRotation = Quaternion.Euler(CurrentGrip.localEulerAngles);
            prop.transform.localScale = CurrentGrip.localScale;
        }
        else
        {
            prop.transform.localPosition = Vector3.zero;
            prop.transform.localRotation = Quaternion.identity;
            // масштаб оставляем как у пропа
        }

        // аниматор
        if (animator)
        {
            if (!string.IsNullOrEmpty(carryBoolParam))
                animator.SetBool(carryBoolParam, true);

            if (!string.IsNullOrEmpty(carryTypeParam))
            {
                int type = 0;
                if (CurrentGrip != null)
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
        }

        // IK (опционально, если используешь Animation Rigging)
#if USING_ANIMATION_RIGGING
        ApplyIKTargets(true);
#endif

        // скорость
        currentMoveMul = (CurrentGrip ? CurrentGrip.moveSpeedMul : baseMoveSpeedMul);
    }

    public void Detach()
    {
        // IK off
#if USING_ANIMATION_RIGGING
        ApplyIKTargets(false);
#endif
        // аниматор
        if (animator)
        {
            if (!string.IsNullOrEmpty(carryBoolParam))
                animator.SetBool(carryBoolParam, false);
        }

        // отцепить/удалить проп
        if (CurrentProp)
        {
            // тут не уничтожаем — let caller decide
            CurrentProp.transform.SetParent(null);
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
