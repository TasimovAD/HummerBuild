// Assets/_Game/Common/AnimatorLayerWeightDriver.cs
using UnityEngine;

[DefaultExecutionOrder(60)]
public class AnimatorLayerWeightDriver : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Имя bool-параметра, который включает переноску")]
    public string carryBoolParam = "Carry";

    [Tooltip("Имя слоя, вес которого хотим крутить")]
    public string layerName = "CarryUpperBody";

    [Tooltip("Скорость сглаживания включения/выключения слоя")]
    public float lerpSpeed = 10f;

    int _carryHash;
    int _layerIndex = -1;
    float _weight;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _carryHash = Animator.StringToHash(carryBoolParam);

        // находим индекс слоя по имени
        _layerIndex = animator ? animator.GetLayerIndex(layerName) : -1;
        if (_layerIndex < 0)
            Debug.LogWarning($"[AnimatorLayerWeightDriver] Layer '{layerName}' не найден у {name}");
    }

    void Update()
    {
        if (!animator || _layerIndex < 0) return;

        bool carry = animator.GetBool(_carryHash);
        float target = carry ? 1f : 0f;

        _weight = Mathf.Lerp(_weight, target, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        animator.SetLayerWeight(_layerIndex, _weight);
    }
}
