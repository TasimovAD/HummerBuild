using UnityEngine;
using Invector.vCharacterController; // 👈 вот этого не хватало

// Требует Invector TPC
public class InvectorCarrySpeedAdapter : MonoBehaviour
{
    [Header("Refs")]
    public PlayerCarryController carry;          // твой контроллер переноски
    public vThirdPersonController invectorTPC;   // компонент Invector на персонаже

    [Header("Tuning")]
    [Range(0.05f, 10f)] public float blendSpeed = 6f; // насколько быстро меняем множитель

    float _currentMul = 1f;

    void Reset()
    {
        carry = GetComponent<PlayerCarryController>();
        invectorTPC = GetComponent<vThirdPersonController>();
    }

    void Update()
    {
        if (!invectorTPC || !carry) return;

        // если несём — берём коэффициент из CarryGrip.moveSpeedMul, иначе 1
        float target = carry.IsCarrying ? Mathf.Clamp(carry.currentMoveMul, 0.1f, 2f) : 1f;

        // сглаживаем (приятнее для управления)
        _currentMul = Mathf.Lerp(_currentMul, target, Time.deltaTime * blendSpeed);

        // применяем к Invector
        invectorTPC.speedMultiplier = _currentMul; // поле в инспекторе "Speed Multiplierы"
    }
}
