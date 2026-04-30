using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackRange = 1.8f;
    public float chaseSpeed = 4f;

    [Header("Attack")]
    public int damage = 10;
    public float attackCooldown = 1.5f;

    private NavMeshAgent _agent;
    private Transform _player;
    private State _state = State.Patrol;
    private int _waypointIndex = 0;
    private float _attackTimer = 0f;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
        GoToNextWaypoint();
    }

    void Update()
    {
        if (_player == null) return;
        _attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange)
            SetState(State.Attack);
        else if (dist <= detectionRange)
            SetState(State.Chase);
        else
            SetState(State.Patrol);

        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
        }
    }

    void SetState(State s)
    {
        if (_state == s) return;
        _state = s;
        _agent.speed = s == State.Chase ? chaseSpeed : patrolSpeed;
    }

    void UpdatePatrol()
    {
        if (waypoints.Length == 0) return;
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            GoToNextWaypoint();
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        _agent.SetDestination(waypoints[_waypointIndex].position);
        _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void UpdateChase()
    {
        _agent.SetDestination(_player.position);
    }

    void UpdateAttack()
    {
        _agent.SetDestination(transform.position);
        transform.LookAt(_player);

        if (_attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            _player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
