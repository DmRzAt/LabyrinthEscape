using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CorridorEnemySpawner : MonoBehaviour
{
	[Serializable]
	private class PatrolZone
	{
		[SerializeField]
		private string _name = "Patrol Zone";

		[SerializeField]
		private AutoPatrolArea _autoPatrolArea;

		[SerializeField]
		private Transform[] _spawnPoints;

		[SerializeField]
		private Transform[] _patrolWaypoints;

		public string Name => _name;

		public Transform[] SpawnPoints
		{
			get
			{
				if (!(_autoPatrolArea != null))
				{
					return _spawnPoints;
				}
				return _autoPatrolArea.SpawnPoints;
			}
		}

		public Transform[] PatrolWaypoints
		{
			get
			{
				if (!(_autoPatrolArea != null))
				{
					return _patrolWaypoints;
				}
				return _autoPatrolArea.Waypoints;
			}
		}

		public bool HasSpawnPoints
		{
			get
			{
				if (SpawnPoints != null)
				{
					return SpawnPoints.Length != 0;
				}
				return false;
			}
		}
	}

	[Header("Spawn")]
	[SerializeField]
	private GameObject[] _enemyPrefabs;

	[SerializeField]
	private Transform[] _spawnPoints;

	[SerializeField]
	private Transform _enemiesContainer;

	[SerializeField]
	[Range(0f, 5f)]
	private float _spawnDelay = 0.25f;

	[SerializeField]
	private bool _spawnOnStart = true;

	[SerializeField]
	private float _navMeshSampleRadius = 2f;

	[SerializeField]
	private bool _usePreplacedEnemies;

	[Header("Spawn Safety")]
	[SerializeField]
	[Range(0f, 30f)]
	private float _minDistanceFromPlayer = 8f;

	[SerializeField]
	[Range(0f, 3f)]
	private float _visibilityCheckHeight = 1.2f;

	[SerializeField]
	[Range(10f, 180f)]
	private float _playerViewAngle = 110f;

	[SerializeField]
	private LayerMask _spawnVisibilityMask = -1;

	[SerializeField]
	private bool _allowVisibleFallback = true;

	[Header("Patrol")]
	[SerializeField]
	private Transform[] _patrolWaypoints;

	[SerializeField]
	[Range(1f, 50f)]
	private float _detectionRange = 6f;

	[SerializeField]
	private bool _chasePlayerImmediately;

	[Header("Patrol Zones")]
	[SerializeField]
	private PatrolZone[] _patrolZones;

	[Header("Auto (BoxCollider area)")]
	[SerializeField]
	private AutoPatrolArea _autoPatrolArea;

	[SerializeField]
	[Range(0f, 16f)]
	private int _autoEnemyCount;

	[Header("Debug")]
	public bool showDebugGizmos = true;

	[SerializeField]
	private bool _debugLogs = true;

	private bool _spawned;

	private Transform _player;

	private readonly RaycastHit[] _visibilityHits = new RaycastHit[16];

	private Transform[] _sectionSource;

	private List<Transform[]> _sections;

	private List<bool> _sectionClaimed;

	private const float PatrolGizmoHeight = 1.2f;

	private void Reset()
	{
		AutoAssignSceneReferences();
	}

	private void Start()
	{
		AutoAssignSceneReferences();
		if (_spawnOnStart)
		{
			Spawn();
		}
	}

	public void Spawn()
	{
		if (_spawned)
		{
			Log("Spawn skipped: zone already activated.");
			return;
		}
		AutoAssignSceneReferences();
		if (_usePreplacedEnemies)
		{
			_spawned = true;
			ActivatePreplacedEnemies();
		}
		else if (_enemyPrefabs == null || _enemyPrefabs.Length == 0)
		{
			Debug.LogError("[" + GetZoneName() + "] Enemy prefab is missing in " + base.name, this);
		}
		else if (!HasAnySpawnPoints() && _autoPatrolArea == null)
		{
			Debug.LogError("[" + GetZoneName() + "] Spawn points are missing in " + base.name, this);
		}
		else
		{
			_spawned = true;
			Log("Spawning enemies...");
			StartCoroutine(SpawnEnemies());
		}
	}

	private IEnumerator SpawnEnemies()
	{
		int spawned = 0;
		_sectionSource = null;
		if (_autoPatrolArea != null && _autoEnemyCount > 0)
		{
			yield return SpawnAutoEnemies(delegate(int count)
			{
				spawned += count;
			});
			Log($"Spawn complete: {spawned} enemies");
			yield break;
		}
		if (HasPatrolZones())
		{
			yield return SpawnZoneEnemies(delegate(int count)
			{
				spawned += count;
			});
			Log($"Spawn complete: {spawned} enemies");
			yield break;
		}
		for (int i = 0; i < _spawnPoints.Length; i++)
		{
			if (SpawnEnemyFromCandidates(_spawnPoints, _patrolWaypoints, i, _spawnPoints.Length))
			{
				spawned++;
			}
			if (_spawnDelay > 0f)
			{
				yield return new WaitForSeconds(_spawnDelay);
			}
		}
		Log($"Spawn complete: {spawned} enemies");
	}

	private IEnumerator SpawnAutoEnemies(Action<int> spawnedCallback)
	{
		Transform[] spawn = _autoPatrolArea.SpawnPoints;
		Transform[] waypoints = _autoPatrolArea.Waypoints;
		if (spawn == null || spawn.Length == 0)
		{
			yield break;
		}
		int spawned = 0;
		for (int i = 0; i < _autoEnemyCount; i++)
		{
			if (SpawnEnemyFromCandidates(spawn, waypoints, i, _autoEnemyCount))
			{
				spawned++;
			}
			if (_spawnDelay > 0f)
			{
				yield return new WaitForSeconds(_spawnDelay);
			}
		}
		spawnedCallback?.Invoke(spawned);
	}

	private IEnumerator SpawnZoneEnemies(Action<int> spawnedCallback)
	{
		int spawnedCount = 0;
		int spawned = 0;
		PatrolZone[] patrolZones = _patrolZones;
		foreach (PatrolZone zone in patrolZones)
		{
			if (zone == null || !zone.HasSpawnPoints)
			{
				continue;
			}
			Transform[] zoneSpawnPoints = zone.SpawnPoints;
			for (int i = 0; i < zoneSpawnPoints.Length; i++)
			{
				if (SpawnEnemyFromCandidates(zoneSpawnPoints, zone.PatrolWaypoints, spawnedCount, zoneSpawnPoints.Length))
				{
					spawned++;
				}
				spawnedCount++;
				if (_spawnDelay > 0f)
				{
					yield return new WaitForSeconds(_spawnDelay);
				}
			}
		}
		spawnedCallback?.Invoke(spawned);
	}

	private bool SpawnEnemyFromCandidates(Transform[] candidates, Transform[] patrolWaypoints, int prefabIndex, int enemyCount)
	{
		if (!TryChooseSpawnPoint(candidates, prefabIndex, out var spawnPoint, out var spawnPos))
		{
			return false;
		}
		return SpawnEnemy(spawnPoint, spawnPos, patrolWaypoints, prefabIndex, enemyCount);
	}

	private bool SpawnEnemy(Transform spawnPoint, Vector3 spawnPos, Transform[] patrolWaypoints, int prefabIndex, int enemyCount)
	{
		if (spawnPoint == null)
		{
			return false;
		}
		GameObject gameObject = _enemyPrefabs[UnityEngine.Random.Range(0, _enemyPrefabs.Length)];
		if (gameObject == null)
		{
			Debug.LogError("[" + GetZoneName() + "] Enemy prefab is missing in " + base.name, this);
			return false;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, spawnPos, spawnPoint.rotation);
		gameObject2.name = gameObject.name + "_" + GetZoneName() + "_" + (prefabIndex + 1);
		gameObject2.SetActive(value: true);
		ParentEnemy(gameObject2);
		if (!EnsureAgentOnNavMesh(gameObject2, spawnPos))
		{
			UnityEngine.Object.Destroy(gameObject2);
			return false;
		}
		EnemyAI componentInChildren = gameObject2.GetComponentInChildren<EnemyAI>();
		if (componentInChildren != null)
		{
			componentInChildren.ConfigureForWave(GetPatrolSection(spawnPos, patrolWaypoints, enemyCount), _detectionRange, _chasePlayerImmediately);
		}
		Log("Spawned enemy at " + spawnPoint.name);
		return true;
	}

	private Transform[] GetPatrolSection(Vector3 spawnPos, Transform[] patrolWaypoints, int enemyCount)
	{
		EnsureSections(patrolWaypoints, enemyCount);
		if (_sections == null || _sections.Count == 0)
		{
			return patrolWaypoints;
		}
		int num = NearestSectionIndex(spawnPos, requireUnclaimed: true);
		if (num < 0)
		{
			num = NearestSectionIndex(spawnPos, requireUnclaimed: false);
		}
		else
		{
			_sectionClaimed[num] = true;
		}
		return _sections[num];
	}

	private int NearestSectionIndex(Vector3 pos, bool requireUnclaimed)
	{
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < _sections.Count; i++)
		{
			if (!requireUnclaimed || !_sectionClaimed[i])
			{
				float num2 = SectionDistanceSqr(_sections[i], pos);
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	private void EnsureSections(Transform[] patrolWaypoints, int enemyCount)
	{
		if (_sections != null && _sectionSource == patrolWaypoints)
		{
			return;
		}
		_sectionSource = patrolWaypoints;
		_sections = new List<Transform[]>();
		_sectionClaimed = new List<bool>();
		if (patrolWaypoints == null)
		{
			return;
		}
		List<Transform> list = OrderIntoLoop(patrolWaypoints);
		int count = list.Count;
		switch (count)
		{
		case 0:
			return;
		case 1:
			_sections.Add(list.ToArray());
			_sectionClaimed.Add(item: false);
			return;
		}
		int num = Mathf.Clamp(enemyCount, 1, count / 2);
		for (int i = 0; i < num; i++)
		{
			int num2 = Mathf.RoundToInt((float)i * (float)count / (float)num);
			int num3 = Mathf.RoundToInt((float)(i + 1) * (float)count / (float)num);
			if (num3 - num2 < 2)
			{
				num3 = num2 + 2;
			}
			List<Transform> list2 = new List<Transform>();
			for (int j = num2; j < num3; j++)
			{
				list2.Add(list[j % count]);
			}
			_sections.Add(list2.ToArray());
			_sectionClaimed.Add(item: false);
		}
	}

	private static float SectionDistanceSqr(Transform[] section, Vector3 pos)
	{
		float num = float.MaxValue;
		foreach (Transform transform in section)
		{
			if (transform != null)
			{
				num = Mathf.Min(num, (transform.position - pos).sqrMagnitude);
			}
		}
		return num;
	}

	private List<Transform> OrderIntoLoop(Transform[] waypoints)
	{
		List<Transform> list = new List<Transform>();
		foreach (Transform transform in waypoints)
		{
			if (transform != null)
			{
				list.Add(transform);
			}
		}
		List<Transform> list2 = new List<Transform>();
		if (list.Count == 0)
		{
			return list2;
		}
		Transform transform2 = list[0];
		list.RemoveAt(0);
		list2.Add(transform2);
		while (list.Count > 0)
		{
			int index = 0;
			float num = float.MaxValue;
			for (int j = 0; j < list.Count; j++)
			{
				float distance;
				float num2 = (TryGetNavMeshRouteDistance(transform2.position, list[j].position, out distance) ? distance : (transform2.position - list[j].position).magnitude);
				if (num2 < num)
				{
					num = num2;
					index = j;
				}
			}
			transform2 = list[index];
			list.RemoveAt(index);
			list2.Add(transform2);
		}
		return list2;
	}

	private static bool TryGetNavMeshRouteDistance(Vector3 from, Vector3 to, out float distance)
	{
		distance = 0f;
		NavMeshPath navMeshPath = new NavMeshPath();
		if (!NavMesh.CalculatePath(from, to, -1, navMeshPath) || navMeshPath.status != 0)
		{
			return false;
		}
		for (int i = 0; i < navMeshPath.corners.Length - 1; i++)
		{
			distance += Vector3.Distance(navMeshPath.corners[i], navMeshPath.corners[i + 1]);
		}
		return true;
	}

	private bool TryChooseSpawnPoint(Transform[] candidates, int preferredIndex, out Transform spawnPoint, out Vector3 spawnPos)
	{
		spawnPoint = null;
		spawnPos = Vector3.zero;
		if (candidates == null || candidates.Length == 0)
		{
			return false;
		}
		Transform transform = null;
		Vector3 vector = Vector3.zero;
		int num = ((candidates.Length != 0) ? (Mathf.Abs(preferredIndex) % candidates.Length) : 0);
		for (int i = 0; i < candidates.Length; i++)
		{
			Transform transform2 = candidates[(num + i) % candidates.Length];
			if (!(transform2 == null) && TryGetNavMeshPosition(transform2.position, out var result, transform2.name) && IsFarEnoughFromPlayer(result))
			{
				if (!CanPlayerSeeSpawn(result))
				{
					spawnPoint = transform2;
					spawnPos = result;
					return true;
				}
				if (transform == null)
				{
					transform = transform2;
					vector = result;
				}
			}
		}
		if (_allowVisibleFallback && transform != null)
		{
			Debug.LogWarning("[" + GetZoneName() + "] No hidden spawn point found. Using distant visible point: " + transform.name, this);
			spawnPoint = transform;
			spawnPos = vector;
			return true;
		}
		Debug.LogWarning("[" + GetZoneName() + "] Spawn skipped: no point is far enough and hidden from the player.", this);
		return false;
	}

	private bool IsFarEnoughFromPlayer(Vector3 spawnPos)
	{
		Transform player = GetPlayer();
		if (player == null || _minDistanceFromPlayer <= 0f)
		{
			return true;
		}
		float num = _minDistanceFromPlayer * _minDistanceFromPlayer;
		return (spawnPos - player.position).sqrMagnitude >= num;
	}

	private bool CanPlayerSeeSpawn(Vector3 spawnPos)
	{
		Transform player = GetPlayer();
		if (player == null)
		{
			return false;
		}
		Camera main = Camera.main;
		int num;
		Vector3 vector;
		if (main != null)
		{
			num = (main.transform.IsChildOf(player) ? 1 : 0);
			if (num != 0)
			{
				vector = main.transform.position;
				goto IL_005b;
			}
		}
		else
		{
			num = 0;
		}
		vector = player.position + Vector3.up * _visibilityCheckHeight;
		goto IL_005b;
		IL_005b:
		Vector3 vector2 = vector;
		Vector3 from = ((num != 0) ? main.transform.forward : player.forward);
		Vector3 to = spawnPos + Vector3.up * _visibilityCheckHeight - vector2;
		float magnitude = to.magnitude;
		if (magnitude <= 0.01f)
		{
			return true;
		}
		if (Vector3.Angle(from, to) > _playerViewAngle * 0.5f)
		{
			return false;
		}
		int num2 = Physics.RaycastNonAlloc(vector2, to.normalized, _visibilityHits, magnitude, _spawnVisibilityMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < num2; i++)
		{
			Transform transform = _visibilityHits[i].collider.transform;
			if (!(transform == player) && !transform.IsChildOf(player))
			{
				return false;
			}
		}
		return true;
	}

	private Transform GetPlayer()
	{
		if (_player != null)
		{
			return _player;
		}
		_player = EnemyAI.PlayerTransform;
		return _player;
	}

	private bool TryGetNavMeshPosition(Vector3 source, out Vector3 result, string pointName)
	{
		if (NavMesh.SamplePosition(source, out var hit, _navMeshSampleRadius, -1))
		{
			result = hit.position;
			return true;
		}
		Debug.LogWarning("[" + GetZoneName() + "] Spawn point is not on NavMesh: " + pointName, this);
		result = source;
		return false;
	}

	private bool EnsureAgentOnNavMesh(GameObject enemyObject, Vector3 spawnPos)
	{
		NavMeshAgent componentInChildren = enemyObject.GetComponentInChildren<NavMeshAgent>();
		if (componentInChildren == null)
		{
			return true;
		}
		componentInChildren.enabled = true;
		if (!componentInChildren.isOnNavMesh)
		{
			componentInChildren.Warp(spawnPos);
		}
		if (componentInChildren.isOnNavMesh)
		{
			return true;
		}
		enemyObject.SetActive(value: false);
		Debug.LogError("[" + GetZoneName() + "] Spawned enemy is not on NavMesh and was disabled: " + enemyObject.name, enemyObject);
		return false;
	}

	private void ParentEnemy(GameObject enemyObject)
	{
		if (_enemiesContainer == null)
		{
			_enemiesContainer = FindChildInZone("Enemies");
		}
		if (_enemiesContainer != null)
		{
			enemyObject.transform.SetParent(_enemiesContainer, worldPositionStays: true);
		}
	}

	private void ActivatePreplacedEnemies()
	{
		if (_enemiesContainer == null)
		{
			_enemiesContainer = FindChildInZone("Enemies");
		}
		if (_enemiesContainer == null)
		{
			Debug.LogError("[" + GetZoneName() + "] Enemies container is missing for preplaced enemies.", this);
			return;
		}
		int num = 0;
		EnemyAI[] componentsInChildren = _enemiesContainer.GetComponentsInChildren<EnemyAI>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value: true);
			componentsInChildren[i].aiActive = true;
			num++;
		}
		Log($"Spawn complete: {num} preplaced enemies activated");
	}

	private bool HasAnySpawnPoints()
	{
		if ((_spawnPoints == null || _spawnPoints.Length == 0) && _autoPatrolArea == null)
		{
			_spawnPoints = FindSpawnPointsInZone();
		}
		if (_spawnPoints != null && _spawnPoints.Length != 0)
		{
			return true;
		}
		return HasPatrolZones();
	}

	private bool HasPatrolZones()
	{
		if (_patrolZones == null || _patrolZones.Length == 0)
		{
			return false;
		}
		PatrolZone[] patrolZones = _patrolZones;
		foreach (PatrolZone patrolZone in patrolZones)
		{
			if (patrolZone != null && patrolZone.HasSpawnPoints)
			{
				return true;
			}
		}
		return false;
	}

	private void AutoAssignSceneReferences()
	{
		if (_enemiesContainer == null)
		{
			_enemiesContainer = FindChildInZone("Enemies");
		}
		if ((_spawnPoints == null || _spawnPoints.Length == 0) && _autoPatrolArea == null)
		{
			_spawnPoints = FindSpawnPointsInZone();
		}
	}

	private Transform FindChildInZone(string childName)
	{
		Transform transform = ((base.transform.parent != null) ? base.transform.parent : base.transform);
		Transform transform2 = transform.Find(childName);
		if (transform2 != null)
		{
			return transform2;
		}
		Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].name == childName)
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}

	private Transform[] FindSpawnPointsInZone()
	{
		Transform transform = FindChildInZone("SpawnPoints");
		if (transform == null)
		{
			return _spawnPoints;
		}
		List<Transform> list = new List<Transform>();
		for (int i = 0; i < transform.childCount; i++)
		{
			list.Add(transform.GetChild(i));
		}
		return list.ToArray();
	}

	private string GetZoneName()
	{
		if (base.transform.parent != null)
		{
			return base.transform.parent.name;
		}
		return base.name;
	}

	private void Log(string message)
	{
		if (_debugLogs)
		{
			Debug.Log("[" + GetZoneName() + "] " + message, this);
		}
	}

	private void OnDrawGizmos()
	{
		if (showDebugGizmos)
		{
			DrawSpawnPointGizmos(_spawnPoints);
			if (_autoPatrolArea != null)
			{
				DrawSpawnPointGizmos(_autoPatrolArea.SpawnPoints);
				DrawPatrolPointGizmos(_autoPatrolArea.Waypoints);
			}
			DrawPatrolPointGizmos(_patrolWaypoints);
		}
	}

	private void DrawSpawnPointGizmos(Transform[] points)
	{
		if (points == null)
		{
			return;
		}
		for (int i = 0; i < points.Length; i++)
		{
			if (!(points[i] == null))
			{
				NavMeshHit hit;
				bool num = NavMesh.SamplePosition(points[i].position, out hit, _navMeshSampleRadius, -1);
				Gizmos.color = (num ? Color.green : Color.red);
				Gizmos.DrawSphere(points[i].position, 0.25f);
				if (num)
				{
					Gizmos.DrawLine(points[i].position, hit.position);
					Gizmos.DrawWireSphere(hit.position, 0.35f);
				}
			}
		}
	}

	private void DrawPatrolPointGizmos(Transform[] points)
	{
		if (points == null)
		{
			return;
		}
		Gizmos.color = Color.cyan;
		Vector3 vector = Vector3.up * 1.2f;
		NavMeshPath path = new NavMeshPath();
		for (int i = 0; i < points.Length; i++)
		{
			if (!(points[i] == null))
			{
				Gizmos.DrawWireSphere(points[i].position + vector, 0.22f);
				Transform transform = points[(i + 1) % points.Length];
				if (transform != null)
				{
					DrawNavMeshRoute(points[i].position, transform.position, path, vector);
				}
			}
		}
	}

	private static void DrawNavMeshRoute(Vector3 a, Vector3 b, NavMeshPath path, Vector3 lift)
	{
		if (NavMesh.CalculatePath(a, b, -1, path) && path.corners.Length > 1)
		{
			for (int i = 0; i < path.corners.Length - 1; i++)
			{
				Gizmos.DrawLine(path.corners[i] + lift, path.corners[i + 1] + lift);
			}
		}
		else
		{
			Gizmos.DrawLine(a + lift, b + lift);
		}
	}
}
