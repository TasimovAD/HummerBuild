using UnityEngine;

public enum CarryStyle
{
    OneHand,        // одна рука
    TwoHandFront,   // двумя руками спереди
    Shoulder        // на плече
}

/// Описание хвата и позиционирования пропа в руке/руках.
public class CarryGrip : MonoBehaviour
{
    [Header("Основной хват (относительно HandCarrySocket)")]
    public CarryStyle style = CarryStyle.OneHand;

    [Tooltip("Локальная позиция относительно HandCarrySocket")]
    public Vector3 localPosition = Vector3.zero;

    [Tooltip("Локальный поворот относительно HandCarrySocket")]
    public Vector3 localEulerAngles = Vector3.zero;

    [Tooltip("Локальный масштаб пропа (если надо подогнать)")]
    public Vector3 localScale = Vector3.one;

    [Header("Опционально: IK цели для рук (Animation Rigging)")]
    public Transform rightHandIKTarget;   // цель для правой руки
    public Transform leftHandIKTarget;    // цель для левой руки

    [Header("Коэффициенты скорости")]
    [Tooltip("Во сколько раз замедлять ИГРОКА при переноске этого предмета")]
    [Range(0.1f, 2f)] public float playerSpeedMul = 0.8f;

    [Tooltip("Во сколько раз замедлять РАБОЧЕГО при переноске этого предмета")]
    [Range(0.1f, 2f)] public float workerSpeedMul = 0.8f;

    // ---- Legacy (для автоматической миграции со старого поля moveSpeedMul) ----
    [SerializeField, HideInInspector] float moveSpeedMul = 0.8f;

#if UNITY_EDITOR
    void OnValidate()
    {
        // если проект ещё использует старое поле — скопируем в новые,
        // но только если новые ещё не трогали (по умолчанию 0.8f)
        const float def = 0.8f;
        if (Mathf.Abs(moveSpeedMul - def) > 0.0001f &&
            Mathf.Abs(playerSpeedMul - def) < 0.0001f &&
            Mathf.Abs(workerSpeedMul - def) < 0.0001f)
        {
            playerSpeedMul = moveSpeedMul;
            workerSpeedMul = moveSpeedMul;
        }
    }
#endif
}
