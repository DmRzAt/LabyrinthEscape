using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
	public enum State
	{
		Idle,
		Patrol,
		Alert,
		Chase,
		Attack,
		Search,
		ReturnToPatrol,
		HitReact,
		Dead
	}

	[Header("Patrol")]
	public Transform[] waypoints;

	public float patrolSpeed = 1.7f;

	[Header("Detection")]
	public float detectionRange = 10f;

	public float attackRange = 1.8f;

	public float chaseSpeed = 3.3f;

	[SerializeField]
	[Range(0f, 2f)]
	private float _alertAnimationLock = 0.45f;

	[SerializeField]
	[Range(10f, 180f)]
	private float _viewAngle = 100f;

	[SerializeField]
	[Range(0f, 10f)]
	private float _closeAwarenessRange = 2.5f;

	[SerializeField]
	private bool _requireLineOfSight = true;

	[SerializeField]
	private LayerMask _playerMask = -1;

	[SerializeField]
	private Transform _eyePoint;

	[SerializeField]
	private bool _chasePlayerImmediately;

	[SerializeField]
	private bool debugLogs;

	[Header("Detection meter (gradual)")]
	[Tooltip("Seconds of clear line-of-sight at point-blank range to fully detect. Farther = slower fill.")]
	[SerializeField]
	[Range(0.1f, 5f)]
	private float _timeToDetect = 1f;

	[Tooltip("How fast the alert meter drains per second when the player is not perceived.")]
	[SerializeField]
	[Range(0.05f, 5f)]
	private float _detectionDecay = 0.5f;

	[Tooltip("Meter level (0..1) above which the enemy turns suspicious and investigates.")]
	[SerializeField]
	[Range(0.05f, 0.95f)]
	private float _suspicionThreshold = 0.3f;

	[Tooltip("Narrower field of view while patrolling/idle (reduced peripheral awareness). Full _viewAngle is used once suspicious or engaged.")]
	[SerializeField]
	[Range(10f, 180f)]
	private float _patrolViewAngle = 60f;

	[Tooltip("Current alert level 0..1 (read-only, exposed for debugging).")]
	[SerializeField]
	private float _detectionMeter;

	[Header("Zone activation")]
	[Tooltip("When false the enemy only patrols and ignores the player. EnemyZoneTrigger toggles this. Zone 1 enemies may stay true.")]
	public bool aiActive = true;

	[Header("Senses / speeds")]
	[SerializeField]
	private float alertSpeed = 2.2f;

	[SerializeField]
	private float forgetTime = 4f;

	[SerializeField]
	private float searchTime = 3f;

	[SerializeField]
	[Range(0f, 10f)]
	private float searchRadius = 4f;

	[Header("Movement Feel")]
	[SerializeField]
	[Range(1f, 40f)]
	private float _agentAcceleration = 18f;

	[SerializeField]
	[Range(30f, 720f)]
	private float _agentAngularSpeed = 360f;

	[SerializeField]
	[Range(0f, 1f)]
	private float _patrolStoppingDistance = 0.15f;

	[SerializeField]
	[Range(0f, 1.5f)]
	private float _alertStoppingDistance = 0.35f;

	[SerializeField]
	[Range(0f, 0.5f)]
	private float _animationSpeedDampTime = 0.08f;

	[SerializeField]
	private bool _useManualMoveRotation = true;

	[SerializeField]
	[Range(1f, 20f)]
	private float _moveTurnSpeed = 8f;

	[SerializeField]
	[Range(0.01f, 0.5f)]
	private float _stopVelocityEpsilon = 0.05f;

	[Header("Memory")]
	[SerializeField]
	private float _searchDuration = 4f;

	[SerializeField]
	private float _chaseRepathInterval = 0.2f;

	[SerializeField]
	private float _alertRepathInterval = 0.35f;

	[SerializeField]
	private float _chaseRepathThreshold = 0.5f;

	[SerializeField]
	[Range(0.05f, 3f)]
	private float _destinationRefreshDistance = 0.4f;

	[SerializeField]
	[Range(0.5f, 6f)]
	private float _navMeshSampleRadius = 3f;

	[Header("Attack")]
	public int damage = 10;

	public float attackCooldown = 1.5f;

	[SerializeField]
	[Range(1f, 20f)]
	private float _turnSpeed = 10f;

	[SerializeField]
	private LayerMask _sightObstacleMask = -1;

	[Header("Stuck Recovery")]
	[SerializeField]
	[Range(0.25f, 5f)]
	private float _stuckCheckTime = 1f;

	[SerializeField]
	[Range(0.01f, 1f)]
	private float _stuckMinMoveDistance = 0.15f;

	[Header("Group AI")]
	[SerializeField]
	private bool _groupBehaviour = true;

	[SerializeField]
	[Range(0f, 50f)]
	private float _alertShareRadius = 12f;

	[SerializeField]
	[Range(1f, 8f)]
	private int _maxSimultaneousAttackers = 2;

	[Tooltip("How often (seconds) the per-enemy group queries (attack-slot claim + alert sharing) run. These scan every other enemy, so recomputing them each frame is O(n²); a few times a second is plenty.")]
	[SerializeField]
	[Range(0.05f, 1f)]
	private float _groupUpdateInterval = 0.2f;

	private NavMeshAgent _agent;

	private Animator _animator;

	private Transform _player;

	private PlayerHealth _playerHealth;

	private State _state = State.Patrol;

	private int _waypointIndex;

	private float _attackTimer;

	private float _searchTimer;

	private Vector3 _lastKnownPlayerPos;

	private float _repathTimer;

	private float _alertRepathTimer;

	private Vector3 _lastChaseTarget;

	private float _findPlayerTimer;

	private float _staggerTimer;

	private float _alertLockTimer;

	private bool _hasSeenPlayer;

	private bool _alertedOnce;

	private int _lastAttackIndex = -1;

	private Vector3 _currentDestination;

	private bool _hasCurrentDestination;

	private NavMeshPathStatus _lastPathStatus = NavMeshPathStatus.PathInvalid;

	private NavMeshPath _path;

	private float _stuckTimer;

	private Vector3 _lastStuckCheckPosition;

	private int _stuckRepathAttempts;

	private bool _hasSpeedParam;

	private bool _hasAttackParam;

	private bool _hasAttackIndexParam;

	private bool _hasHurtParam;

	private bool _hasAlertParam;

	private EnemyAudio _audio;

	private float _stepAccum;

	[Header("Perception throttle")]
	[Tooltip("Recompute the costly sight/hearing checks every N frames (1 = every frame).")]
	[SerializeField]
	[Range(1f, 6f)]
	private int _perceptionInterval = 3;

	private int _perceptionCounter;

	private bool _cachedCanSee;

	private bool _cachedCloseAware;

	private static Transform s_player;

	private static PlayerHealth s_playerHealth;

	private static readonly List<EnemyAI> _all = new List<EnemyAI>();

	private float _surroundAngleOffset;

	private float _groupTimer;

	private bool _cachedCanClaimSlot = true;

	public bool IsStaggered => _staggerTimer > 0f;

	public State CurrentState => _state;

	public static Transform PlayerTransform
	{
		get
		{
			ResolvePlayer();
			return s_player;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Color color = Color.green;
		if (_state == State.Alert || _state == State.Search || _state == State.ReturnToPatrol)
		{
			color = Color.yellow;
		}
		else if (_state == State.Chase || _state == State.Attack)
		{
			color = Color.red;
		}
		else if (_state == State.Dead)
		{
			color = Color.gray;
		}
		Gizmos.color = color;
		Gizmos.DrawWireSphere(base.transform.position, detectionRange);
		Gizmos.color = new Color(1f, 0.75f, 0f, 1f);
		Gizmos.DrawWireSphere(base.transform.position, _closeAwarenessRange);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, attackRange);
		Vector3 from = ((_eyePoint != null) ? _eyePoint.position : (base.transform.position + Vector3.up * 1.5f));
		Vector3 vector = ((_eyePoint != null) ? _eyePoint.forward : base.transform.forward);
		Quaternion quaternion = Quaternion.AngleAxis((0f - _viewAngle) * 0.5f, Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(_viewAngle * 0.5f, Vector3.up);
		Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
		Gizmos.DrawRay(from, (quaternion * vector).normalized * detectionRange);
		Gizmos.DrawRay(from, (quaternion2 * vector).normalized * detectionRange);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(_lastKnownPlayerPos, 0.35f);
		if (_hasCurrentDestination)
		{
			Gizmos.color = ((_lastPathStatus == NavMeshPathStatus.PathComplete) ? Color.green : Color.red);
			Gizmos.DrawLine(base.transform.position, _currentDestination);
			Gizmos.DrawWireSphere(_currentDestination, 0.3f);
		}
		if (HasWaypoints())
		{
			Gizmos.color = Color.cyan;
			NavMeshPath navMeshPath = new NavMeshPath();
			for (int i = 0; i < waypoints.Length; i++)
			{
				if (waypoints[i] == null)
				{
					continue;
				}
				Gizmos.DrawWireSphere(waypoints[i].position, 0.25f);
				Transform transform = waypoints[(i + 1) % waypoints.Length];
				if (transform == null)
				{
					continue;
				}
				if (NavMesh.CalculatePath(waypoints[i].position, transform.position, -1, navMeshPath) && navMeshPath.corners.Length > 1)
				{
					for (int j = 0; j < navMeshPath.corners.Length - 1; j++)
					{
						Gizmos.DrawLine(navMeshPath.corners[j], navMeshPath.corners[j + 1]);
					}
				}
				else
				{
					Gizmos.DrawLine(waypoints[i].position, transform.position);
				}
			}
		}
#if UNITY_EDITOR
		Handles.color = Color.white;
		Handles.Label(base.transform.position + Vector3.up * 2.2f, $"{_state} / alert {_detectionMeter * 100f:0}% / {_lastPathStatus}");
#endif
	}

	public static void NotifyNoise(Vector3 worldPos, float loudness)
	{
		for (int i = 0; i < _all.Count; i++)
		{
			EnemyAI enemyAI = _all[i];
			if (enemyAI != null)
			{
				enemyAI.HearNoise(worldPos, loudness);
			}
		}
	}

	public void HearNoise(Vector3 worldPos, float loudness)
	{
		if (!aiActive || _state == State.Dead)
		{
			return;
		}
		if (!((worldPos - base.transform.position).sqrMagnitude > loudness * loudness))
		{
			_lastKnownPlayerPos = GetNearestNavMeshPoint(worldPos);
			if (_searchTimer < searchTime)
			{
				_searchTimer = searchTime;
			}
			_detectionMeter = Mathf.Max(_detectionMeter, _suspicionThreshold + 0.05f);
			SetState(State.Alert);
			if (!TrySetDestination(_lastKnownPlayerPos, "heard noise"))
			{
				SetState(State.Search);
			}
			Log("heard noise and became alert");
		}
	}

	public void Stagger(float duration)
	{
		if (!(duration <= 0f))
		{
			_staggerTimer = Mathf.Max(_staggerTimer, duration);
			CancelInvoke("AnimationEvent_AttackHit");
			_attackTimer = Mathf.Max(_attackTimer, duration + 0.15f);
			if (_animator == null)
			{
				_animator = GetComponentInChildren<Animator>();
			}
			if (_hasHurtParam)
			{
				_animator.SetTrigger("Hurt");
			}
			StopAgentMovement(clearPath: false);
		}
	}

	private void OnEnable()
	{
		if (!_all.Contains(this))
		{
			_all.Add(this);
		}
		_surroundAngleOffset = UnityEngine.Random.Range(0f, 360f);
	}

	private void OnDisable()
	{
		_all.Remove(this);
	}

	private void Start()
	{
		_agent = GetComponent<NavMeshAgent>();
		_animator = GetComponentInChildren<Animator>();
		if (_animator != null)
		{
			_animator.applyRootMotion = false;
		}
		_audio = GetComponent<EnemyAudio>();
		if (_audio == null)
		{
			_audio = base.gameObject.AddComponent<EnemyAudio>();
		}
		_perceptionCounter = UnityEngine.Random.Range(1, Mathf.Max(2, _perceptionInterval + 1));
		CacheAnimatorParams();
		_path = new NavMeshPath();
		_agent.speed = patrolSpeed;
		_agent.updatePosition = true;
		_agent.updateRotation = !_useManualMoveRotation;
		_agent.acceleration = _agentAcceleration;
		_agent.angularSpeed = _agentAngularSpeed;
		_agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
		_agent.avoidancePriority = UnityEngine.Random.Range(20, 80);
		_agent.autoBraking = true;
		_agent.autoRepath = true;
		_agent.stoppingDistance = _patrolStoppingDistance;
		ConfigureRigidbodyForAgent();
		_lastStuckCheckPosition = base.transform.position;
		TryFindPlayer(immediate: true);
		GoToNextWaypoint();
	}

	private bool AgentReady()
	{
		if (_agent != null)
		{
			return _agent.isOnNavMesh;
		}
		return false;
	}

	private void ConfigureRigidbodyForAgent()
	{
		Rigidbody component = GetComponent<Rigidbody>();
		if (!(component == null))
		{
			if (!component.isKinematic)
			{
				component.linearVelocity = Vector3.zero;
				component.angularVelocity = Vector3.zero;
			}
			component.useGravity = false;
			component.isKinematic = true;
			component.interpolation = RigidbodyInterpolation.None;
		}
	}

	private void UpdateMovementPresentation()
	{
		if (_agent == null)
		{
			return;
		}
		float num = 0f;
		bool flag = AgentReady() && !_agent.isStopped && _state != State.Attack && _state != State.Dead && _state != State.HitReact && _alertLockTimer <= 0f;
		if (flag)
		{
			Vector3 velocity = _agent.velocity;
			velocity.y = 0f;
			num = velocity.magnitude;
			if (num < _stopVelocityEpsilon)
			{
				num = 0f;
			}
		}
		if (_animator != null && _hasSpeedParam)
		{
			_animator.SetFloat("Speed", num, _animationSpeedDampTime, Time.deltaTime);
		}
		if (_audio != null && num > 0.05f)
		{
			_stepAccum += num * Time.deltaTime;
			if (_stepAccum >= _audio.stride)
			{
				_stepAccum = 0f;
				_audio.PlayStep();
			}
		}
		RotateTowardsMoveDirection(flag);
	}

	private void RotateTowardsMoveDirection(bool canShowMovement)
	{
		if (_useManualMoveRotation && canShowMovement)
		{
			Vector3 vector = ((_agent.desiredVelocity.sqrMagnitude > 0.01f) ? _agent.desiredVelocity : _agent.velocity);
			vector.y = 0f;
			if (!(vector.sqrMagnitude <= _stopVelocityEpsilon * _stopVelocityEpsilon))
			{
				Quaternion b = Quaternion.LookRotation(vector.normalized);
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, _moveTurnSpeed * Time.deltaTime);
			}
		}
	}

	private void StopAgentMovement(bool clearPath)
	{
		if (AgentReady())
		{
			_agent.isStopped = true;
			_agent.velocity = Vector3.zero;
			if (clearPath && _agent.hasPath)
			{
				_agent.ResetPath();
				_hasCurrentDestination = false;
			}
		}
	}

	private void LateUpdate()
	{
		UpdateMovementPresentation();
	}

	private void Update()
	{
		if (_agent == null)
		{
			return;
		}
		if (_staggerTimer > 0f)
		{
			_staggerTimer -= Time.deltaTime;
			_state = State.HitReact;
			StopAgentMovement(clearPath: false);
			return;
		}
		if (_alertLockTimer > 0f)
		{
			_alertLockTimer -= Time.deltaTime;
			if (!aiActive || !(_player != null) || !((base.transform.position - _player.position).sqrMagnitude <= attackRange * attackRange))
			{
				StopAgentMovement(clearPath: false);
				return;
			}
			_alertLockTimer = 0f;
		}
		if (!aiActive)
		{
			if (HasWaypoints())
			{
				SetState(State.Patrol);
				UpdatePatrol();
			}
			else
			{
				SetState(State.Idle);
			}
			return;
		}
		if (_player == null)
		{
			TryFindPlayer(immediate: false);
			if (_player == null)
			{
				if (HasWaypoints())
				{
					SetState(State.Patrol);
					UpdatePatrol();
				}
				else
				{
					SetState(State.Idle);
				}
				return;
			}
		}
		_attackTimer -= Time.deltaTime;
		float num = Vector3.Distance(base.transform.position, _player.position);
		if (--_perceptionCounter <= 0)
		{
			_perceptionCounter = Mathf.Max(1, _perceptionInterval);
			_cachedCanSee = _chasePlayerImmediately || CanSeePlayer(num);
			_cachedCloseAware = !_cachedCanSee && CanCloseAwarePlayer(num);
		}
		bool cachedCanSee = _cachedCanSee;
		bool cachedCloseAware = _cachedCloseAware;
		bool detected = cachedCanSee;
		UpdateDetectionMeter(num, cachedCanSee, cachedCloseAware);
		if (_detectionMeter >= 1f && (cachedCanSee || cachedCloseAware || _chasePlayerImmediately))
		{
			if (!_hasSeenPlayer)
			{
				_hasSeenPlayer = true;
				if (!_alertedOnce)
				{
					_alertedOnce = true;
					PlayAlertAnimation();
				}
				Log("locked on player");
			}
			_lastKnownPlayerPos = GetNearestNavMeshPoint(_player.position);
			_searchTimer = forgetTime + searchTime;
			bool flag = false;
			if (_groupBehaviour)
			{
				_groupTimer -= Time.deltaTime;
				if (_groupTimer <= 0f)
				{
					_groupTimer = _groupUpdateInterval;
					flag = true;
				}
			}
			bool flag2 = num <= attackRange && (!_groupBehaviour || (flag ? (_cachedCanClaimSlot = CanClaimAttackSlot()) : _cachedCanClaimSlot));
			SetState(flag2 ? State.Attack : State.Chase);
			if (flag)
			{
				ShareAlert(_lastKnownPlayerPos);
			}
		}
		else if (cachedCanSee || cachedCloseAware || _detectionMeter >= _suspicionThreshold)
		{
			if (!_alertedOnce && (cachedCanSee || cachedCloseAware))
			{
				_alertedOnce = true;
				PlayAlertAnimation();
				Log("became suspicious");
			}
			if (cachedCanSee || cachedCloseAware)
			{
				_lastKnownPlayerPos = GetNearestNavMeshPoint(_player.position);
			}
			SetState(State.Alert);
		}
		else if (_hasSeenPlayer && _searchTimer > 0f)
		{
			_searchTimer -= Time.deltaTime;
			float num2 = Vector3.Distance(base.transform.position, _lastKnownPlayerPos);
			if (_searchTimer > searchTime)
			{
				SetState(State.Chase);
			}
			else
			{
				if (num2 <= Mathf.Max(1.5f, _agent.stoppingDistance + 0.2f))
				{
					Log("reached last known player position");
				}
				SetState((num2 > 1.5f) ? State.Alert : State.Search);
			}
		}
		else
		{
			_hasSeenPlayer = false;
			_alertedOnce = false;
			if (_state != State.Patrol && _state != State.ReturnToPatrol)
			{
				SetState(HasWaypoints() ? State.ReturnToPatrol : State.Idle);
			}
		}
		switch (_state)
		{
		case State.Patrol:
			UpdatePatrol();
			break;
		case State.ReturnToPatrol:
			UpdateReturnToPatrol();
			break;
		case State.Alert:
			UpdateGoTo(_lastKnownPlayerPos);
			break;
		case State.Chase:
			UpdateChase(detected);
			break;
		case State.Search:
			UpdateSearch();
			break;
		case State.Attack:
			UpdateAttack();
			break;
		}
		CheckStuck();
	}

	private void SetState(State s)
	{
		if (_state == s)
		{
			return;
		}
		State state = _state;
		_state = s;
		if (_agent != null)
		{
			switch (s)
			{
			case State.Chase:
				_agent.speed = chaseSpeed;
				break;
			case State.Alert:
			case State.Search:
			case State.ReturnToPatrol:
				_agent.speed = alertSpeed;
				break;
			default:
				_agent.speed = patrolSpeed;
				break;
			}
			switch (s)
			{
			case State.Chase:
			case State.Attack:
				_agent.stoppingDistance = Mathf.Min(0.3f, attackRange * 0.25f);
				break;
			case State.Alert:
			case State.Search:
			case State.ReturnToPatrol:
				_agent.stoppingDistance = _alertStoppingDistance;
				break;
			default:
				_agent.stoppingDistance = _patrolStoppingDistance;
				break;
			}
		}
		if (s != State.Attack && AgentReady() && _agent.isStopped)
		{
			_agent.isStopped = false;
		}
		if (s == State.Chase)
		{
			_repathTimer = 0f;
		}
		if (s == State.Alert || s == State.Search || s == State.ReturnToPatrol)
		{
			_alertRepathTimer = 0f;
		}
		if (s == State.ReturnToPatrol && state != State.ReturnToPatrol)
		{
			Log("returned to patrol");
		}
	}

	private void UpdatePatrol()
	{
		if (HasWaypoints() && AgentReady() && !_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance < 0.5f))
		{
			GoToNextWaypoint();
		}
	}

	private void GoToNextWaypoint()
	{
		if (!HasWaypoints() || !AgentReady())
		{
			return;
		}
		int waypointIndex = _waypointIndex;
		int num = -1;
		for (int i = 0; i < waypoints.Length; i++)
		{
			int num2 = (waypointIndex + i) % waypoints.Length;
			Transform transform = waypoints[num2];
			if (waypoints.Length > 1 && IsAtWaypoint(num2))
			{
				num = num2;
			}
			else if (transform != null && TrySetDestination(transform.position, "patrol waypoint"))
			{
				_waypointIndex = (num2 + 1) % waypoints.Length;
				return;
			}
		}
		if (num >= 0)
		{
			_waypointIndex = (num + 1) % waypoints.Length;
		}
		else
		{
			Log("no reachable patrol waypoint found");
		}
	}

	private void UpdateChase(bool detected)
	{
		if (!AgentReady())
		{
			return;
		}
		_repathTimer -= Time.deltaTime;
		Vector3 vector = (detected ? GetSurroundPosition() : _lastKnownPlayerPos);
		if (_repathTimer <= 0f || (vector - _lastChaseTarget).sqrMagnitude > _chaseRepathThreshold * _chaseRepathThreshold)
		{
			if (!TrySetDestination(vector, detected ? "chase player" : "last known player position"))
			{
				SetState(detected ? State.Alert : State.Search);
				return;
			}
			_lastChaseTarget = vector;
			_repathTimer = _chaseRepathInterval;
		}
	}

	private void UpdateGoTo(Vector3 dest)
	{
		if (!AgentReady())
		{
			return;
		}
		_alertRepathTimer -= Time.deltaTime;
		if (!(_alertRepathTimer > 0f) || ShouldRefreshDestination(dest))
		{
			if (!TrySetDestination(dest, "alert/search destination"))
			{
				SetState(State.Search);
			}
			else
			{
				_alertRepathTimer = _alertRepathInterval;
			}
		}
	}

	private void UpdateReturnToPatrol()
	{
		if (!AgentReady() || !HasWaypoints())
		{
			SetState(State.Idle);
			return;
		}
		int nearestReachableWaypointIndex = GetNearestReachableWaypointIndex();
		if (nearestReachableWaypointIndex < 0)
		{
			SetState(State.Search);
			return;
		}
		Transform transform = waypoints[nearestReachableWaypointIndex];
		if (transform != null && ShouldRefreshDestination(transform.position))
		{
			TrySetDestination(transform.position, "return to patrol");
		}
		if (AtNearestWaypoint())
		{
			_waypointIndex = GetNextWaypointIndex(nearestReachableWaypointIndex);
			SetState(State.Patrol);
		}
	}

	private bool AtNearestWaypoint()
	{
		if (!HasWaypoints() || !AgentReady())
		{
			return true;
		}
		Transform transform = waypoints[GetNearestWaypointIndex()];
		if (!(transform == null))
		{
			return (base.transform.position - transform.position).sqrMagnitude < 2.25f;
		}
		return true;
	}

	private Vector3 GetSurroundPosition()
	{
		float num = Mathf.Clamp(attackRange * 0.65f, 0.5f, attackRange - 0.3f);
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < _all.Count; i++)
		{
			EnemyAI enemyAI = _all[i];
			if (!(enemyAI == null) && !(enemyAI._player == null) && enemyAI.aiActive && !(enemyAI._player != _player) && !((enemyAI.transform.position - base.transform.position).sqrMagnitude > _alertShareRadius * _alertShareRadius) && (enemyAI._state == State.Chase || enemyAI._state == State.Attack))
			{
				if (enemyAI == this)
				{
					num2 = num3;
				}
				num3++;
			}
		}
		Vector3 vector;
		if (num3 <= 1)
		{
			vector = base.transform.position - _player.position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.0001f)
			{
				vector = base.transform.forward;
			}
			vector.Normalize();
		}
		else
		{
			float f = (_surroundAngleOffset + 360f / (float)num3 * (float)num2) * (MathF.PI / 180f);
			vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
		}
		if (NavMesh.SamplePosition(_player.position + vector * num, out var hit, Mathf.Max(num, _navMeshSampleRadius), -1))
		{
			return hit.position;
		}
		return _player.position;
	}

	private bool CanClaimAttackSlot()
	{
		int num = 0;
		for (int i = 0; i < _all.Count; i++)
		{
			EnemyAI enemyAI = _all[i];
			if (!(enemyAI == null) && !(enemyAI == this) && enemyAI.aiActive && !(enemyAI._player != _player) && !((enemyAI.transform.position - _player.position).sqrMagnitude > (attackRange + 1f) * (attackRange + 1f)) && enemyAI._state == State.Attack)
			{
				num++;
			}
		}
		return num < _maxSimultaneousAttackers;
	}

	private void ShareAlert(Vector3 pos)
	{
		float num = _alertShareRadius * _alertShareRadius;
		for (int i = 0; i < _all.Count; i++)
		{
			EnemyAI enemyAI = _all[i];
			if (!(enemyAI == null) && !(enemyAI == this) && !((enemyAI.transform.position - base.transform.position).sqrMagnitude > num) && enemyAI._searchTimer < _searchDuration * 0.5f)
			{
				enemyAI.AlertTo(pos);
			}
		}
	}

	private void UpdateSearch()
	{
		if (AgentReady() && !_agent.pathPending && (!_agent.hasPath || (!float.IsInfinity(_agent.remainingDistance) && _agent.remainingDistance < 0.6f)))
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * searchRadius;
			if (NavMesh.SamplePosition(_lastKnownPlayerPos + new Vector3(vector.x, 0f, vector.y), out var hit, searchRadius, -1))
			{
				TrySetDestination(hit.position, "search point");
			}
		}
	}

	private void UpdateAttack()
	{
		if (_player == null)
		{
			SetState(State.Search);
			return;
		}
		if (Vector3.Distance(base.transform.position, _player.position) > attackRange + 0.25f)
		{
			SetState(State.Chase);
			return;
		}
		StopAgentMovement(clearPath: true);
		RotateTowardsPlayer();
		if ((!(_playerHealth != null) || !_playerHealth.IsDead) && _attackTimer <= 0f)
		{
			_attackTimer = attackCooldown;
			PlayAttackAnimation();
			CancelInvoke("AnimationEvent_AttackHit");
		}
	}

	public void AnimationEvent_AttackHit()
	{
		if (!(this == null) && base.gameObject.activeInHierarchy && !(_player == null) && !(_playerHealth == null) && !_playerHealth.IsDead && !(Vector3.Distance(base.transform.position, _player.position) > attackRange + 0.5f))
		{
			_playerHealth.ReceiveAttack(damage, this);
		}
	}

	private void RotateTowardsPlayer()
	{
		Vector3 position = _player.position;
		position.y = base.transform.position.y;
		Vector3 forward = position - base.transform.position;
		if (!(forward.sqrMagnitude <= 0.0001f))
		{
			Quaternion b = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, _turnSpeed * Time.deltaTime);
		}
	}

	private void UpdateDetectionMeter(float dist, bool sees, bool closeAware)
	{
		if (_chasePlayerImmediately)
		{
			_detectionMeter = 1f;
			return;
		}
		float num = Mathf.Max(0.1f, _timeToDetect);
		if (sees)
		{
			float num2 = Mathf.Clamp01(1f - dist / Mathf.Max(0.01f, detectionRange));
			_detectionMeter += (0.4f + 1.6f * num2) / num * Time.deltaTime;
		}
		else if (closeAware)
		{
			_detectionMeter += 1.5f / num * Time.deltaTime;
		}
		else
		{
			_detectionMeter -= _detectionDecay * Time.deltaTime;
		}
		_detectionMeter = Mathf.Clamp01(_detectionMeter);
	}

	private float EffectiveViewAngle()
	{
		if (!(_detectionMeter >= _suspicionThreshold) && !_hasSeenPlayer && _state != State.Alert && _state != State.Search && _state != State.Chase && _state != State.Attack)
		{
			return _patrolViewAngle;
		}
		return _viewAngle;
	}

	private bool CanSeePlayer(float distanceToPlayer)
	{
		if (distanceToPlayer > detectionRange)
		{
			return false;
		}
		Vector3 vector = ((_eyePoint != null) ? _eyePoint.position : (base.transform.position + Vector3.up * 1.5f));
		Vector3 from = ((_eyePoint != null) ? _eyePoint.forward : base.transform.forward);
		Vector3 to = _player.position + Vector3.up * 1f - vector;
		if (Vector3.Angle(from, to) > EffectiveViewAngle() * 0.5f)
		{
			return false;
		}
		return HasLineOfSightToPlayerBody();
	}

	private bool CanCloseAwarePlayer(float distanceToPlayer)
	{
		if (distanceToPlayer > _closeAwarenessRange)
		{
			return false;
		}
		return HasLineOfSightToPlayerBody();
	}

	private bool HasLineOfSightToPlayerBody()
	{
		if (_player == null)
		{
			return false;
		}
		if (HasLineOfSightToPosition(_player.position + Vector3.up * 1f))
		{
			return true;
		}
		if (HasLineOfSightToPosition(_player.position + Vector3.up * 1.7f))
		{
			return true;
		}
		if (HasLineOfSightToPosition(_player.position + Vector3.up * 0.3f))
		{
			return true;
		}
		return false;
	}

	private bool HasLineOfSightToPosition(Vector3 target)
	{
		Vector3 vector = ((_eyePoint != null) ? _eyePoint.position : (base.transform.position + Vector3.up * 1.5f));
		return HasLineOfSight(vector, target - vector);
	}

	private bool HasLineOfSight(Vector3 origin, Vector3 direction)
	{
		if (!_requireLineOfSight)
		{
			return true;
		}
		float magnitude = direction.magnitude;
		if (magnitude <= 0.001f)
		{
			return true;
		}
		Vector3 vector = direction / magnitude;
		Vector3 origin2 = origin + vector * 0.2f;
		float num = magnitude - 0.2f;
		if (num <= 0f)
		{
			return true;
		}
		if (Physics.Raycast(origin2, vector, out var hitInfo, num, _sightObstacleMask, QueryTriggerInteraction.Ignore))
		{
			if (!hitInfo.collider.CompareTag("Player"))
			{
				return hitInfo.collider.GetComponentInParent<PlayerHealth>() != null;
			}
			return true;
		}
		return true;
	}

	private void TryFindPlayer(bool immediate)
	{
		if (!immediate)
		{
			_findPlayerTimer -= Time.deltaTime;
			if (_findPlayerTimer > 0f)
			{
				return;
			}
			_findPlayerTimer = 1f;
		}
		ResolvePlayer();
		_player = s_player;
		_playerHealth = s_playerHealth;
	}

	private static void ResolvePlayer()
	{
		if (!(s_player != null))
		{
			GameObject gameObject = GameObject.FindWithTag("Player");
			if (gameObject != null)
			{
				s_player = gameObject.transform;
				s_playerHealth = gameObject.GetComponent<PlayerHealth>();
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		s_player = null;
		s_playerHealth = null;
		_all.Clear();
	}

	public void AlertTo(Vector3 worldPosition)
	{
		if (aiActive)
		{
			_lastKnownPlayerPos = GetNearestNavMeshPoint(worldPosition);
			_searchTimer = forgetTime + searchTime;
			_detectionMeter = Mathf.Max(_detectionMeter, _suspicionThreshold + 0.05f);
			SetState(State.Alert);
			if (!TrySetDestination(_lastKnownPlayerPos, "shared alert"))
			{
				SetState(State.Search);
			}
		}
	}

	public void ReactToDamage(Vector3 attackerPosition)
	{
		if (_state != State.Dead)
		{
			aiActive = true;
			_lastKnownPlayerPos = GetNearestNavMeshPoint(attackerPosition);
			_searchTimer = forgetTime + searchTime;
			_hasSeenPlayer = true;
			_detectionMeter = 1f;
			SetState(State.Chase);
			if (!TrySetDestination(_lastKnownPlayerPos, "damage reaction"))
			{
				SetState(State.Search);
			}
			Log("took damage and reacted to attacker position");
		}
	}

	public void MarkDead()
	{
		_state = State.Dead;
		aiActive = false;
	}

	public void ConfigureForWave(Transform[] patrolWaypoints, float newDetectionRange, bool chasePlayerImmediately)
	{
		if (patrolWaypoints != null && patrolWaypoints.Length != 0)
		{
			waypoints = patrolWaypoints;
			int nearestWaypointIndex = GetNearestWaypointIndex();
			_waypointIndex = (IsAtWaypoint(nearestWaypointIndex) ? GetNextWaypointIndex(nearestWaypointIndex) : nearestWaypointIndex);
		}
		detectionRange = newDetectionRange;
		_chasePlayerImmediately = chasePlayerImmediately;
		aiActive = true;
		if (_agent != null && !_chasePlayerImmediately)
		{
			GoToNextWaypoint();
		}
	}

	private bool HasWaypoints()
	{
		if (waypoints != null)
		{
			return waypoints.Length != 0;
		}
		return false;
	}

	private int GetNearestWaypointIndex()
	{
		if (!HasWaypoints())
		{
			return 0;
		}
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < waypoints.Length; i++)
		{
			if (!(waypoints[i] == null))
			{
				float sqrMagnitude = (waypoints[i].position - base.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = i;
				}
			}
		}
		return result;
	}

	private int GetNextWaypointIndex(int index)
	{
		if (!HasWaypoints())
		{
			return 0;
		}
		return (index + 1) % waypoints.Length;
	}

	private bool IsAtWaypoint(int index)
	{
		if (!HasWaypoints() || index < 0 || index >= waypoints.Length || waypoints[index] == null)
		{
			return false;
		}
		float num = ((_agent != null) ? Mathf.Max(0.5f, _agent.stoppingDistance + 0.25f) : 0.75f);
		return (base.transform.position - waypoints[index].position).sqrMagnitude <= num * num;
	}

	private int GetNearestReachableWaypointIndex()
	{
		if (!HasWaypoints() || !AgentReady())
		{
			return -1;
		}
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < waypoints.Length; i++)
		{
			if (waypoints[i] == null || !NavMesh.SamplePosition(waypoints[i].position, out var hit, _navMeshSampleRadius, -1))
			{
				continue;
			}
			if (_path == null)
			{
				_path = new NavMeshPath();
			}
			if (_agent.CalculatePath(hit.position, _path) && _path.status == NavMeshPathStatus.PathComplete)
			{
				float sqrMagnitude = (hit.position - base.transform.position).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					num = i;
				}
			}
		}
		if (num < 0)
		{
			Log("no reachable waypoint for return to patrol");
		}
		return num;
	}

	private Vector3 GetNearestNavMeshPoint(Vector3 source)
	{
		if (NavMesh.SamplePosition(source, out var hit, _navMeshSampleRadius, -1))
		{
			return hit.position;
		}
		Log($"NavMesh.SamplePosition failed near {source}");
		return source;
	}

	private bool TrySetDestination(Vector3 target, string reason)
	{
		if (!AgentReady())
		{
			return false;
		}
		if (!NavMesh.SamplePosition(target, out var hit, _navMeshSampleRadius, -1))
		{
			_lastPathStatus = NavMeshPathStatus.PathInvalid;
			Log($"path invalid ({reason}): target is not near NavMesh {target}");
			return false;
		}
		if (_path == null)
		{
			_path = new NavMeshPath();
		}
		bool flag = _agent.CalculatePath(hit.position, _path);
		_lastPathStatus = (flag ? _path.status : NavMeshPathStatus.PathInvalid);
		if (!flag || _path.status != 0)
		{
			Log($"path invalid ({reason}): {_lastPathStatus} to {hit.position}");
			return false;
		}
		_currentDestination = hit.position;
		_hasCurrentDestination = true;
		if (!_agent.SetDestination(hit.position))
		{
			_lastPathStatus = NavMeshPathStatus.PathInvalid;
			Log($"path request failed ({reason}) to {hit.position}");
			return false;
		}
		return true;
	}

	private bool ShouldRefreshDestination(Vector3 target)
	{
		if (!_hasCurrentDestination)
		{
			return true;
		}
		if (_agent == null || !_agent.hasPath)
		{
			return true;
		}
		if (_agent.pathPending)
		{
			return false;
		}
		return (target - _currentDestination).sqrMagnitude > _destinationRefreshDistance * _destinationRefreshDistance;
	}

	private void CheckStuck()
	{
		if (!AgentReady() || _state == State.Attack || _state == State.Dead || _state == State.HitReact)
		{
			ResetStuckCheck();
			return;
		}
		if (!_agent.hasPath || _agent.pathPending || (!float.IsInfinity(_agent.remainingDistance) && _agent.remainingDistance <= _agent.stoppingDistance + 0.25f))
		{
			ResetStuckCheck();
			return;
		}
		_stuckTimer += Time.deltaTime;
		if (_stuckTimer < _stuckCheckTime)
		{
			return;
		}
		float num = Vector3.Distance(base.transform.position, _lastStuckCheckPosition);
		_lastStuckCheckPosition = base.transform.position;
		_stuckTimer = 0f;
		if (num >= _stuckMinMoveDistance)
		{
			_stuckRepathAttempts = 0;
			return;
		}
		_stuckRepathAttempts++;
		Log($"enemy stuck, repath attempt {_stuckRepathAttempts}");
		if (!_hasCurrentDestination || _stuckRepathAttempts > 2 || !TrySetDestination(_currentDestination, "stuck repath"))
		{
			_stuckRepathAttempts = 0;
			SetState((_searchTimer > 0f) ? State.Search : State.ReturnToPatrol);
		}
	}

	private void ResetStuckCheck()
	{
		_stuckTimer = 0f;
		_stuckRepathAttempts = 0;
		_lastStuckCheckPosition = base.transform.position;
	}

	private void Log(string message)
	{
		if (debugLogs)
		{
			Debug.Log("[EnemyAI:" + base.name + "] " + message, this);
		}
	}

	private void CacheAnimatorParams()
	{
		_hasSpeedParam = false;
		_hasAttackParam = false;
		_hasAttackIndexParam = false;
		_hasHurtParam = false;
		_hasAlertParam = false;
		if (_animator == null || _animator.runtimeAnimatorController == null)
		{
			return;
		}
		AnimatorControllerParameter[] parameters = _animator.parameters;
		foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
		{
			if (animatorControllerParameter.name == "Speed" && animatorControllerParameter.type == AnimatorControllerParameterType.Float)
			{
				_hasSpeedParam = true;
			}
			else if (animatorControllerParameter.name == "Attack" && animatorControllerParameter.type == AnimatorControllerParameterType.Trigger)
			{
				_hasAttackParam = true;
			}
			else if (animatorControllerParameter.name == "AttackIndex" && animatorControllerParameter.type == AnimatorControllerParameterType.Int)
			{
				_hasAttackIndexParam = true;
			}
			else if (animatorControllerParameter.name == "Hurt" && animatorControllerParameter.type == AnimatorControllerParameterType.Trigger)
			{
				_hasHurtParam = true;
			}
			else if (animatorControllerParameter.name == "Alert" && animatorControllerParameter.type == AnimatorControllerParameterType.Trigger)
			{
				_hasAlertParam = true;
			}
		}
	}

	private void PlayAlertAnimation()
	{
		if (!(_animator == null) && _hasAlertParam && !(_alertLockTimer > 0f) && _state != State.Dead && (!(_player != null) || !((base.transform.position - _player.position).sqrMagnitude <= attackRange * attackRange)))
		{
			_animator.SetTrigger("Alert");
			if (_audio != null)
			{
				_audio.PlayAlert();
			}
			if (!_chasePlayerImmediately)
			{
				_alertLockTimer = _alertAnimationLock;
			}
		}
	}

	private void PlayAttackAnimation()
	{
		if (_animator == null || !_hasAttackParam)
		{
			return;
		}
		if (_hasAttackIndexParam)
		{
			int num = UnityEngine.Random.Range(0, 3);
			if (num == _lastAttackIndex)
			{
				num = (num + 1) % 3;
			}
			_lastAttackIndex = num;
			_animator.SetInteger("AttackIndex", num);
		}
		_animator.SetTrigger("Attack");
		if (_audio != null)
		{
			_audio.PlayAttack();
		}
	}
}
