using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// System AI przeciwników. Patrol → Detect → Chase → Attack.
/// Wymaga NavMeshAgent + ustawionego NavMesh na scenie.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, ReturnToPatrol }

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float fieldOfViewAngle = 120f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float losePlayerDistance = 15f;
    [SerializeField] private float losePlayerTime = 3f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform player;
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private float losePlayerTimer = 0f;
    private float attackTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Szukamy gracza na scenie
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        agent.speed = patrolSpeed;

        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.ReturnToPatrol:
                UpdateReturnToPatrol();
                break;
        }
    }

    // ─────────────── PATROL ───────────────

    private void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (patrolPoints.Length == 0) return;

        // Дійшли до точки патрулювання
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }

    // ─────────────── CHASE ───────────────

    private void UpdateChase()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Атакуємо, якщо близько
        if (distToPlayer <= attackRange)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Переслідуємо
        agent.SetDestination(player.position);

        // Загубили гравця?
        if (!CanSeePlayer())
        {
            losePlayerTimer += Time.deltaTime;
            if (losePlayerTimer >= losePlayerTime)
            {
                ChangeState(EnemyState.ReturnToPatrol);
            }
        }
        else
        {
            losePlayerTimer = 0f;
        }

        // Занадто далеко
        if (distToPlayer > losePlayerDistance)
        {
            ChangeState(EnemyState.ReturnToPatrol);
        }
    }

    // ─────────────── ATTACK ───────────────

    private void UpdateAttack()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Дивимося на гравця
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 5f * Time.deltaTime);

        agent.SetDestination(transform.position); // стоїмо

        if (attackTimer <= 0f)
        {
            // Завдаємо шкоди
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                Debug.Log($"[EnemyAI] Attacked player for {attackDamage} damage!");
            }
            attackTimer = attackCooldown;
        }

        // Гравець втік — гонимося
        if (distToPlayer > attackRange * 1.5f)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    // ─────────────── RETURN TO PATROL ───────────────

    private void UpdateReturnToPatrol()
    {
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    // ─────────────── DETECTION ───────────────

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > detectionRange) return false;

        // Перевірка кута зору
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > fieldOfViewAngle * 0.5f) return false;

        // Raycast — чи нема перешкод
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 playerCenter = player.position + Vector3.up * 1f;

        if (Physics.Raycast(eyePos, (playerCenter - eyePos).normalized, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == player)
                return true;
        }

        return false;
    }

    // ─────────────── STATE CHANGE ───────────────

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Patrol:
                agent.speed = patrolSpeed;
                losePlayerTimer = 0f;
                break;
            case EnemyState.Chase:
                agent.speed = chaseSpeed;
                losePlayerTimer = 0f;
                break;
            case EnemyState.Attack:
                agent.speed = 0f;
                break;
            case EnemyState.ReturnToPatrol:
                agent.speed = patrolSpeed;
                break;
        }

        Debug.Log($"[EnemyAI] {gameObject.name} → {newState}");
    }

    // ─────────────── GIZMOS (для Editor) ───────────────

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV
        Gizmos.color = Color.cyan;
        Vector3 leftBound = Quaternion.Euler(0, -fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, fieldOfViewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBound * detectionRange);
        Gizmos.DrawRay(transform.position, rightBound * detectionRange);
    }
}
