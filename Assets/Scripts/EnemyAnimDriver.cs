using UnityEngine;
using UnityEngine.AI;

// Parameter contract (Animator must match):
//   Float "Speed"  : 0=Idle, 0~2.5=Walk, 2.5+=Run  [range: 0 ~ chaseSpeed(5)]
//   Trigger "Shoot": one trigger per shot, cooldown controlled by EnemyController.fireRate
//   Trigger "Death": one-shot, irreversible, triggers agent+controller shutdown

[RequireComponent(typeof(EnemyController))]
public class EnemyAnimDriver : MonoBehaviour
{
    private EnemyController controller;
    private NavMeshAgent agent;
    private Animator animator;

    private float lastObservedShootTime = float.MinValue;
    private bool deathTriggered;
    private float speedVelocity;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null || controller == null) return;
        if (!controller.enabled) return;

        float rawSpeed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
        float smoothSpeed = Mathf.SmoothDamp(
            animator.GetFloat("Speed"), rawSpeed,
            ref speedVelocity, 0.1f);
        animator.SetFloat("Speed", smoothSpeed);

        if (controller.lastShootTime > lastObservedShootTime)
        {
            lastObservedShootTime = controller.lastShootTime;
            animator.SetTrigger("Shoot");
        }

        if (controller.currentState == EnemyAIState.Dead && !deathTriggered)
        {
            deathTriggered = true;
            animator.SetTrigger("Death");
        }
    }
}
