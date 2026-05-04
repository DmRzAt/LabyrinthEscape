using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Search, Attack }

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackRange = 1.8f;
    public float chaseSpeed = 4f;
    [SerializeField, Range(10f, 180f)] private float _viewAngle = 90f;
    [SerializeField, Range(0f, 10f)] private float _closeAwarenessRange = 3f;
    [SerializeField] private bool _requireLineOfSight = true;
    [SerializeField] private Transform _eyePoint;
    [SerializeField] private bool _chasePlayerImmediately = false;

    [Header("Memory")]
    [SerializeField] private float _searchDuration = 4f;
    [SerializeField] private float _chaseRepathInterval = 0.2f;
    [SerializeField] private float _chaseRepathThreshold = 0.5f;

    [Header("Attack")]
    public int damage = 10;
    public float attackCooldown = 1.5f;
    [SerializeField] private float _attackDamageDelay = 0.4f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private State _state = State.Patrol;
    private int _waypointIndex = 0;
    private float _attackTimer = 0f;

    private float _searchTimer;
    private Vector3 _lastKnownPlayerPos;
    private float _repathTimer;
    private Vector3 _lastChaseTarget;
    private float _findPlayerTimer;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        TryFindPlayer(true);
        GoToNextWaypoint();
    }

    void Update()
    {
        if (_animator != null)
            _animator.SetFloat("Speed", _agent.velocity.magnitude);

        if (_player == null)
        {
            TryFindPlayer(false);
            if (_player == null) { UpdatePatrol(); return; }
        }

        _attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool detected = _chasePlayerImmediately || CanDetectPlayer(dist);

        if (detected)
        {
            _lastKnownPlayerPos = _player.position;
            _searchTimer = _searchDuration;
            SetState(dist <= attackRange ? State.Attack : State.Chase);
        }
        else if (_searchTimer > 0f)
        {
            _searchTimer -= Time.deltaTime;
            SetState(State.Search);
        }
        else
        {
            SetState(State.Patrol);
        }

        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Search: UpdateSearch(); break;
            case State.Attack: UpdateAttack(); break;
        }
    }

    void SetState(State s)
    {
        if (_state == s) return;
        _state = s;
        _agent.speed = (s == State.Chase || s == State.Search) ? chaseSpeed : patrolSpeed;
        if (s == State.Chase) _repathTimer = 0f;
    }

    void UpdatePatrol()
    {
        if (!HasWaypoints()) return;
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            GoToNextWaypoint();
    }

    void GoToNextWaypoint()
    {
        if (!HasWaypoints() || _agent == null) return;
        var wp = waypoints[_waypointIndex];
        if (wp != null) _agent.SetDestination(wp.position);
        _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void UpdateChase()
    {
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f ||
            (_player.position - _lastChaseTarget).sqrMagnitude > _chaseRepathThreshold * _chaseRepathThreshold)
        {
            _agent.SetDestination(_player.position);
            _lastChaseTarget = _player.position;
            _repathTimer = _chaseRepathInterval;
        }
    }

    void UpdateSearch()
    {
        _agent.SetDestination(_lastKnownPlayerPos);
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            _searchTimer = 0f;
    }

    void UpdateAttack()
    {
        _agent.SetDestination(transform.position);
        RotateTowardsPlayer();

        if (_attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            if (_animator != null) _animator.SetTrigger("Attack");
            CancelInvoke(nameof(ApplyAttackDamage));
            Invoke(nameof(ApplyAttackDamage), _attackDamageDelay);
        }
    }

    void ApplyAttackDamage()
    {
        if (_player == null || _playerHealth == null) return;
        if (Vector3.Distance(transform.position, _player.position) > attackRange + 0.5f) return;
        _playerHealth.TakeDamage(damage);
    }

    void RotateTowardsPlayer()
    {
        Vector3 targetPosition = _player.position;
        targetPosition.y = transform.position.y;

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool CanDetectPlayer(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRange) return false;

        Vector3 origin = _eyePoint != null ? _eyePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 forward = _eyePoint != null ? _eyePoint.forward : transform.forward;
        Vector3 target = _player.position + Vector3.up * 1f;
        Vector3 direction = target - origin;

        if (distanceToPlayer <= _closeAwarenessRange)
            return HasLineOfSight(origin, direction);

        if (Vector3.Angle(forward, direction) > _viewAngle * 0.5f)
            return false;

        return HasLineOfSight(origin, direction);
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 direction)
    {
        if (!_requireLineOfSight) return true;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, detectionRange))
            return hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerHealth>() != null;

        return false;
    }

    private void TryFindPlayer(bool immediate)
    {
        if (!immediate)
        {
            _findPlayerTimer -= Time.deltaTime;
            if (_findPlayerTimer > 0f) return;
            _findPlayerTimer = 1f;
        }

        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    public void ConfigureForWave(Transform[] patrolWaypoints, float newDetectionRange, bool chasePlayerImmediately)
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            waypoints = patrolWaypoints;
            _waypointIndex = GetNearestWaypointIndex();
        }

        detectionRange = newDetectionRange;
        _chasePlayerImmediately = chasePlayerImmediately;

        if (_agent != null && !_chasePlayerImmediately)
            GoToNextWaypoint();
    }

    private bool HasWaypoints()
    {
        return waypoints != null && waypoints.Length > 0;
    }

    private int GetNearestWaypointIndex()
    {
        if (!HasWaypoints()) return 0;

        int nearestIndex = 0;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            float distanceSqr = (waypoints[i].position - transform.position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
