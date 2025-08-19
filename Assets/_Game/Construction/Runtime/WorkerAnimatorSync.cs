using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class WorkerAnimatorSync : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public string speedParam = "InputMagnitude"; // или "MoveSpeed" — проверь в контроллере

    void Reset() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent == null || animator == null) return;

        // скорость от NavMesh
        float speed = agent.velocity.magnitude;

        // применяем к параметру в аниматоре
        animator.SetFloat(speedParam, speed);
    }
}
