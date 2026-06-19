using UnityEngine;
using UnityEngine.AI;

public enum EnemyAIState { Patrol, Chase, Attack, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 5f;
    public float patrolSpeed = 1.5f;
    public float detectionRange = 12f;
    public float attackRange = 25f;
    public float fireRate = 1.2f;
    public float aimSpread = 0.08f;
    public int damage = 8;

    [Header("References")]
    public Transform gunMuzzle;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletTrailPrefab;
    public GameObject healthBarCanvas;
    public UnityEngine.UI.Image healthBarFill;

    [HideInInspector] public EnemyAIState currentState;
    [HideInInspector] public float lastShootTime = float.MinValue;

    private int currentHealth;
    private bool isAlert = false;
    private float attackTimer;
    private NavMeshAgent agent;
    private Transform player;
    private Vector3 patrolTarget;
    private float patrolWait;
    private float healthBarTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        player = Camera.main?.transform;
        GameManager.Instance?.RegisterEnemy(this);
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
        PickPatrolTarget();
    }

    public void Init(Vector3 spawnPos)
    {
        transform.position = spawnPos;
        currentHealth = maxHealth;
        isAlert = false;
        agent.speed = patrolSpeed;
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
        healthBarTimer = 0f;
        PickPatrolTarget();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameOver) return;
        if (player == null) { player = Camera.main?.transform; return; }
        if (healthBarCanvas != null && healthBarCanvas.activeSelf)
        {
            healthBarCanvas.transform.forward = Camera.main.transform.forward;
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f)
                healthBarCanvas.SetActive(false);
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (isAlert || dist < detectionRange)
            ChaseAndAttack(dist);
        else
            Patrol();
    }

    void ChaseAndAttack(float dist)
    {
        currentState = EnemyAIState.Chase;
        isAlert = true;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        attackTimer += Time.deltaTime;
        float rate = fireRate + Random.Range(0f, fireRate * 0.5f);

        if (attackTimer > rate && dist < attackRange)
        {
            attackTimer = 0;

            if (HasLineOfSight())
                Shoot();
        }
    }

    void Patrol()
    {
        currentState = EnemyAIState.Patrol;
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance < 1.5f)
        {
            patrolWait -= Time.deltaTime;
            if (patrolWait <= 0)
                PickPatrolTarget();
        }
    }

    void PickPatrolTarget()
    {
        var bounds = GameManager.Instance?.mapBounds ?? new Bounds(Vector3.zero, Vector3.one * 96f);
        float m = 5f;
        patrolTarget = new Vector3(
            Random.Range(bounds.min.x + m, bounds.max.x - m),
            0,
            Random.Range(bounds.min.z + m, bounds.max.z - m)
        );
        patrolWait = Random.Range(2f, 6f);
        agent.SetDestination(patrolTarget);
    }

    bool HasLineOfSight()
    {
        if (gunMuzzle == null) return true;
        Vector3 origin = gunMuzzle.position;
        Vector3 dir = (player.position - origin).normalized;
        float dist = Vector3.Distance(origin, player.position);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist))
        {
            return hit.collider.CompareTag("Player") ||
                   hit.collider.GetComponentInParent<PlayerHealth>() != null;
        }
        return false;
    }

    void Shoot()
    {
        if (gunMuzzle == null) return;
        currentState = EnemyAIState.Attack;
        lastShootTime = Time.time;

        Vector3 origin = gunMuzzle.position;
        Vector3 dir = (player.position - origin).normalized;

        dir += Random.insideUnitSphere * aimSpread;
        dir.Normalize();

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 200f))
        {
            var ph = hit.collider.GetComponentInParent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damage + Random.Range(0, 8));
        }

        if (muzzleFlashPrefab != null)
            Destroy(Instantiate(muzzleFlashPrefab, origin, Quaternion.identity), 0.05f);

        if (bulletTrailPrefab != null)
        {
            var trail = Instantiate(bulletTrailPrefab);
            var bt = trail.GetComponent<BulletTrail>();
            if (bt != null)
                bt.Init(origin, origin + dir * 100f);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        isAlert = true;
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(true);
            healthBarTimer = 3f;
        }
        UpdateHealthBar();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }

    private bool isDead = false;

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = EnemyAIState.Dead;

        GameManager.Instance?.UnregisterEnemy(this);

        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) { ps.transform.SetParent(null); ps.Play(); Destroy(ps.gameObject, 2f); }

        if (agent != null) agent.enabled = false;
        enabled = false;
        Destroy(gameObject, 0.3f);
    }
}
