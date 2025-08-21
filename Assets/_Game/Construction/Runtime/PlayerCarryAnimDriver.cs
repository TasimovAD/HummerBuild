using UnityEngine;

/// Вешай на игрока (там где Animator).
/// Работает с любым контроллером (Invector/свой) — он только включает bool "Carry".
/// Условие: если в сокете есть ребёнок (ресурс), Carry=true.
public class PlayerCarryAnimDriver : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform handCarrySocket;   // куда кладём ресурс

    [Header("Animator Params")]
    public string carryParam = "Carry";

    bool _lastState;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!animator || !handCarrySocket) return;

        bool carrying = handCarrySocket.childCount > 0;
        if (carrying != _lastState)
        {
            _lastState = carrying;
            animator.SetBool(carryParam, carrying);
        }
    }

    // На случай если хочешь управлять без сокета:
    public void ForceSetCarry(bool value)
    {
        _lastState = value;
        animator?.SetBool(carryParam, value);
    }
}
