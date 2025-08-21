// Assets/_Game/Common/AnimatorLayerWeightDriver.cs
using UnityEngine;

[DefaultExecutionOrder(60)]
public class AnimatorLayerWeightDriver : MonoBehaviour
{
    public Animator animator;
    public string carryBoolParam = "Carry";
    public string carryMoveBoolParam = "CarryMove";   // <— новый
    public string layerName = "CarryUpperBody";

    public float lerpSpeed = 10f;

    int _carryHash, _carryMoveHash;
    int _layerIndex = -1;
    float _weight;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _carryHash     = Animator.StringToHash(carryBoolParam);
        _carryMoveHash = Animator.StringToHash(carryMoveBoolParam);

        _layerIndex = animator ? animator.GetLayerIndex(layerName) : -1;
        if (_layerIndex < 0)
            Debug.LogWarning($"[AnimatorLayerWeightDriver] Layer '{layerName}' не найден у {name}");
    }

    void Update()
    {
        if (!animator || _layerIndex < 0) return;

        bool carry     = animator.GetBool(_carryHash);
        bool carryMove = animator.GetBool(_carryMoveHash);

        // слой нужен ТОЛЬКО когда не движемся: Carry==true && CarryMove==false
        float target = (carry && !carryMove) ? 1f : 0f;

        _weight = Mathf.Lerp(_weight, target, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        animator.SetLayerWeight(_layerIndex, _weight);
    }
}
