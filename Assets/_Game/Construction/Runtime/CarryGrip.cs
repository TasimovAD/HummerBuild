using UnityEngine;

public enum CarryStyle
{
    OneHand,        // одна рука (универсально)
    TwoHandFront,   // двумя руками спереди (мешок цемента и т.п.)
    Shoulder        // на плече (бревно)
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

    [Header("Опционально: IK цели для второй руки (Animation Rigging)")]
    public Transform rightHandIKTarget;   // цель для правой руки
    public Transform leftHandIKTarget;    // цель для левой руки

    [Header("Скоростные коэффициенты (для баланса)")]
    [Range(0.2f, 1f)] public float moveSpeedMul = 0.8f; // замедление при переноске
}
