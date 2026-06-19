using UnityEngine;

// Reads EnemySnapshot from EnemyController. No inference, no judgment.
// Parameter contract (Animator must match):
//   Float "Speed"  : 0=Idle, 0~2.5=Walk, 2.5+=Run
//   Trigger "Shoot": one trigger per shot
//   Trigger "Death": one-shot, irreversible

[RequireComponent(typeof(EnemyController))]
public class EnemyAnimDriver : MonoBehaviour
{
    private EnemyController controller;
    private Animator animator;

    private bool deathTriggered;
    private float speedVelocity;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null || controller == null) return;

        EnemySnapshot snap = controller.snapshot;
        if (snap.isDead && !deathTriggered)
        {
            deathTriggered = true;
            animator.SetTrigger("Death");
            return;
        }
        if (deathTriggered) return;

        float smoothSpeed = Mathf.SmoothDamp(
            animator.GetFloat("Speed"), snap.speed,
            ref speedVelocity, 0.1f);
        animator.SetFloat("Speed", smoothSpeed);

        if (snap.isShooting)
            animator.SetTrigger("Shoot");
    }
}
