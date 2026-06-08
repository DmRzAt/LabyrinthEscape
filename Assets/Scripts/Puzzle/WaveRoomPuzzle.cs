using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveRoomPuzzle : MonoBehaviour
{
	private enum LeverSolutionMode
	{
		AnyOrderAllOn,
		OrderedSequence
	}

	[Serializable]
	public class EnemyWave
	{
		[SerializeField]
		private GameObject[] _enemyPrefabs;

		[Tooltip("How many enemies to spawn this wave. 0 = one per spawn point (legacy). Use 1 for a single boss.")]
		[SerializeField]
		private int _enemyCount;

		public GameObject[] EnemyPrefabs => _enemyPrefabs;

		public int EnemyCount => _enemyCount;
	}

	[Header("Room")]
	[SerializeField]
	private Door _roomDoor;

	[Tooltip("Key-gated entrance door to seal shut during the fight and reopen on completion.")]
	[SerializeField]
	private LockedDoor _lockedRoomDoor;

	[SerializeField]
	private GameObject _floorSymbol;

	[Header("Waves")]
	[SerializeField]
	private EnemyWave[] _waves = new EnemyWave[2];

	[SerializeField]
	private Transform[] _spawnPoints;

	[SerializeField]
	[Range(0f, 5f)]
	private float _spawnDelay = 0.25f;

	[SerializeField]
	private float _navMeshSampleRadius = 2f;

	[Header("Spawn Safety")]
	[SerializeField]
	[Range(0f, 30f)]
	private float _minDistanceFromPlayer = 4.5f;

	[Header("Spawned Enemy AI")]
	[SerializeField]
	private Transform[] _enemyPatrolWaypoints;

	[SerializeField]
	[Range(1f, 50f)]
	private float _spawnedEnemyDetectionRange = 18f;

	[SerializeField]
	private bool _chasePlayerImmediately = true;

	[Header("Auto (BoxCollider area)")]
	[SerializeField]
	private AutoPatrolArea _autoPatrolArea;

	[Header("Levers")]
	[SerializeField]
	private GameObject _leversRoot;

	[SerializeField]
	private PuzzleLever[] _levers = new PuzzleLever[4];

	[SerializeField]
	private LeverSolutionMode _leverSolutionMode;

	[SerializeField]
	private PuzzleLever[] _leverSequence;

	[SerializeField]
	private bool _resetSequenceOnMistake = true;

	[Header("Mistake Penalty")]
	[Tooltip("Enemy spawned when the player pulls a wrong lever in OrderedSequence mode.")]
	[SerializeField]
	private GameObject _mistakeEnemyPrefab;

	[SerializeField]
	private bool _spawnEnemyOnMistake = true;

	[Tooltip("Max penalty enemies alive at once; further mistakes won't spawn more until some die. 0 = unlimited.")]
	[SerializeField]
	[Range(0f, 8f)]
	private int _maxPenaltyEnemies = 2;

	[Header("Hand-off")]
	[Tooltip("When true, clearing the waves does NOT give the reward — it fires WavesCleared and lets another puzzle (e.g. BrazierPuzzle) take over and grant the reward.")]
	[SerializeField]
	private bool _deferRewardToExternal;

	[Tooltip("Safety: if the alive-enemy count makes no progress for this long (e.g. one got stuck off-NavMesh or fell out of the world), force-clear the remaining enemies so the room can't soft-lock. 0 = disabled.")]
	[SerializeField]
	[Range(0f, 120f)]
	private float _stuckClearSeconds = 30f;

	[Header("Reward")]
	[Tooltip("Optional: a chest revealed on completion (hidden until solved). Preferred over a floating key.")]
	[SerializeField]
	private GameObject _rewardChest;

	[SerializeField]
	private GameObject _keyPrefab;

	[SerializeField]
	private Transform _keySpawnPoint;

	[SerializeField]
	[Range(0f, 3f)]
	private float _keySpawnHeightOffset = 1f;

	[SerializeField]
	[Range(0f, 3f)]
	private float _keyPickupDelay = 0.75f;

	[SerializeField]
	private Door[] _doorsToOpenOnComplete;

	[SerializeField]
	private bool _openRoomDoorOnComplete = true;

	[SerializeField]
	private bool _debugLogs = true;

	private readonly List<EnemyHealth> _aliveEnemies = new List<EnemyHealth>();

	private int _activeLeverCount;

	private int _requiredLeverCount;

	private int _sequenceIndex;

	private bool _started;

	private bool _completed;

	private bool _resettingLevers;

	private Transform _player;

	private AudioSource _sfx;

	private AudioClip _correctClip;

	private AudioClip _wrongClip;

	private AudioClip _rewardClip;

	public event Action WavesCleared;

	private void Awake()
	{
		if (_floorSymbol != null)
		{
			_floorSymbol.SetActive(value: false);
		}
		if (_leversRoot != null)
		{
			_leversRoot.SetActive(value: false);
		}
		if (_rewardChest != null)
		{
			_rewardChest.SetActive(value: false);
		}
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (!(puzzleLever == null))
			{
				_requiredLeverCount++;
				puzzleLever.SetOn(isOn: false, notify: false);
				puzzleLever.SetEnabled(isEnabled: false);
				puzzleLever.StateChanged += OnLeverStateChanged;
			}
		}
	}

	private void OnDestroy()
	{
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (puzzleLever != null)
			{
				puzzleLever.StateChanged -= OnLeverStateChanged;
			}
		}
		foreach (EnemyHealth aliveEnemy in _aliveEnemies)
		{
			if (aliveEnemy != null)
			{
				aliveEnemy.Died -= OnEnemyDied;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!_started && other.CompareTag("Player"))
		{
			_started = true;
			CloseAndLockDoor();
			StartCoroutine(RunWaves());
		}
	}

	private IEnumerator RunWaves()
	{
		for (int waveIndex = 0; waveIndex < _waves.Length; waveIndex++)
		{
			yield return SpawnWave(_waves[waveIndex]);
			yield return WaitForWaveCleared();
		}
		bool flag = this.WavesCleared != null;
		this.WavesCleared?.Invoke();
		if (_deferRewardToExternal && flag)
		{
			if (_debugLogs)
			{
				Debug.Log("[WaveRoomPuzzle] Waves cleared — handing off to external puzzle.", this);
			}
			yield break;
		}
		if (_deferRewardToExternal)
		{
			Debug.LogWarning("[WaveRoomPuzzle] Defer is on but nothing handled WavesCleared (external puzzle missing/disabled) — granting the reward myself so the player isn't trapped.", this);
		}
		RevealLeverPuzzle();
	}

	private IEnumerator WaitForWaveCleared()
	{
		float stuckTimer = 0f;
		int lastCount = CountAliveEnemies();
		while (CountAliveEnemies() > 0)
		{
			int num = CountAliveEnemies();
			if (num != lastCount)
			{
				lastCount = num;
				stuckTimer = 0f;
			}
			else
			{
				stuckTimer += Time.deltaTime;
			}
			if (_stuckClearSeconds > 0f && stuckTimer >= _stuckClearSeconds)
			{
				ForceClearStuckEnemies();
				stuckTimer = 0f;
				lastCount = CountAliveEnemies();
			}
			yield return null;
		}
	}

	private void ForceClearStuckEnemies()
	{
		List<EnemyHealth> list = new List<EnemyHealth>(_aliveEnemies);
		int num = 0;
		foreach (EnemyHealth item in list)
		{
			if (!(item == null) && IsEnemyUnrecoverable(item))
			{
				item.TakeDamage(999999);
				num++;
			}
		}
		if (num > 0)
		{
			Debug.LogWarning($"[WaveRoomPuzzle] Force-cleared {num} stuck/out-of-world enemy(ies) to avoid a soft-lock.", this);
		}
	}

	private bool IsEnemyUnrecoverable(EnemyHealth e)
	{
		if (e.transform.position.y < base.transform.position.y - 20f)
		{
			return true;
		}
		NavMeshAgent componentInChildren = e.GetComponentInChildren<NavMeshAgent>();
		if (componentInChildren != null && componentInChildren.enabled && !componentInChildren.isOnNavMesh)
		{
			return true;
		}
		return false;
	}

	private IEnumerator SpawnWave(EnemyWave wave)
	{
		Transform[] activeSpawnPoints = GetActiveSpawnPoints();
		if (wave == null || wave.EnemyPrefabs == null || wave.EnemyPrefabs.Length == 0 || activeSpawnPoints.Length == 0)
		{
			yield break;
		}
		int spawnCount = ((wave.EnemyCount > 0) ? wave.EnemyCount : activeSpawnPoints.Length);
		HashSet<Transform> usedSpawnPoints = new HashSet<Transform>();
		for (int i = 0; i < spawnCount; i++)
		{
			if (usedSpawnPoints.Count >= activeSpawnPoints.Length)
			{
				usedSpawnPoints.Clear();
			}
			GameObject gameObject = wave.EnemyPrefabs[UnityEngine.Random.Range(0, wave.EnemyPrefabs.Length)];
			if (gameObject != null && TryChooseWaveSpawnPoint(activeSpawnPoints, usedSpawnPoints, out var spawnPoint, out var spawnPos))
			{
				usedSpawnPoints.Add(spawnPoint);
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, spawnPos, spawnPoint.rotation);
				if (!EnsureAgentOnNavMesh(gameObject2, spawnPos))
				{
					UnityEngine.Object.Destroy(gameObject2);
					continue;
				}
				EnemyAI componentInChildren = gameObject2.GetComponentInChildren<EnemyAI>();
				if (componentInChildren != null)
				{
					Transform[] patrolWaypoints = ((_autoPatrolArea != null) ? _autoPatrolArea.Waypoints : _enemyPatrolWaypoints);
					componentInChildren.ConfigureForWave(patrolWaypoints, _spawnedEnemyDetectionRange, _chasePlayerImmediately);
				}
				EnemyHealth componentInChildren2 = gameObject2.GetComponentInChildren<EnemyHealth>();
				if (componentInChildren2 != null)
				{
					_aliveEnemies.Add(componentInChildren2);
					componentInChildren2.Died += OnEnemyDied;
				}
			}
			if (_spawnDelay > 0f)
			{
				yield return new WaitForSeconds(_spawnDelay);
			}
		}
	}

	private Transform[] GetActiveSpawnPoints()
	{
		if (_autoPatrolArea != null && _autoPatrolArea.SpawnPoints != null && _autoPatrolArea.SpawnPoints.Length != 0)
		{
			return _autoPatrolArea.SpawnPoints;
		}
		if (_spawnPoints != null && _spawnPoints.Length != 0)
		{
			return _spawnPoints;
		}
		return Array.Empty<Transform>();
	}

	private bool TryGetNavMeshPosition(Vector3 source, out Vector3 result)
	{
		if (NavMesh.SamplePosition(source, out var hit, _navMeshSampleRadius, -1))
		{
			result = hit.position;
			return true;
		}
		Debug.LogWarning($"[WaveRoomPuzzle] No NavMesh near {source}, skipping spawn.", this);
		result = source;
		return false;
	}

	private bool TryChooseWaveSpawnPoint(Transform[] candidates, HashSet<Transform> usedSpawnPoints, out Transform spawnPoint, out Vector3 spawnPos)
	{
		spawnPoint = null;
		spawnPos = Vector3.zero;
		if (candidates == null || candidates.Length == 0)
		{
			return false;
		}
		Transform player = GetPlayer();
		float num = _minDistanceFromPlayer * _minDistanceFromPlayer;
		bool flag = player != null && _minDistanceFromPlayer > 0f;
		Transform transform = null;
		Vector3 vector = Vector3.zero;
		float num2 = float.MinValue;
		Transform transform2 = null;
		Vector3 vector2 = Vector3.zero;
		float num3 = float.MinValue;
		foreach (Transform transform3 in candidates)
		{
			if (!(transform3 == null) && (usedSpawnPoints == null || !usedSpawnPoints.Contains(transform3)) && TryGetNavMeshPosition(transform3.position, out var result))
			{
				float num4 = ((player != null) ? (result - player.position).sqrMagnitude : 0f);
				if ((!flag || num4 >= num) && num4 > num2)
				{
					transform = transform3;
					vector = result;
					num2 = num4;
				}
				if (num4 > num3)
				{
					transform2 = transform3;
					vector2 = result;
					num3 = num4;
				}
			}
		}
		if (transform != null)
		{
			spawnPoint = transform;
			spawnPos = vector;
			return true;
		}
		if (transform2 == null)
		{
			return false;
		}
		if (_debugLogs)
		{
			Debug.Log($"[WaveRoomPuzzle] All unused spawn points are closer than {_minDistanceFromPlayer:0.0}m to the player. Using farthest point: {transform2.name}", this);
		}
		spawnPoint = transform2;
		spawnPos = vector2;
		return true;
	}

	private bool EnsureAgentOnNavMesh(GameObject enemyObject, Vector3 spawnPos)
	{
		if (enemyObject == null)
		{
			return false;
		}
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
		Debug.LogError("[WaveRoomPuzzle] Spawned enemy is not on NavMesh and was removed: " + enemyObject.name, enemyObject);
		return false;
	}

	private int CountAliveEnemies()
	{
		for (int num = _aliveEnemies.Count - 1; num >= 0; num--)
		{
			if (_aliveEnemies[num] == null)
			{
				_aliveEnemies.RemoveAt(num);
			}
		}
		return _aliveEnemies.Count;
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

	private void OnEnemyDied(EnemyHealth enemy)
	{
		if (!(enemy == null))
		{
			enemy.Died -= OnEnemyDied;
			_aliveEnemies.Remove(enemy);
		}
	}

	private void RevealLeverPuzzle()
	{
		if (_floorSymbol != null)
		{
			_floorSymbol.SetActive(value: true);
		}
		if (_leversRoot != null)
		{
			_leversRoot.SetActive(value: true);
		}
		_activeLeverCount = 0;
		_sequenceIndex = 0;
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (puzzleLever != null)
			{
				puzzleLever.SetOn(isOn: false, notify: false);
				puzzleLever.SetEnabled(isEnabled: true);
			}
		}
		if (_requiredLeverCount == 0)
		{
			CompletePuzzle();
		}
	}

	private void OnLeverStateChanged(PuzzleLever lever, bool isOn)
	{
		if (_completed || _resettingLevers)
		{
			return;
		}
		if (_leverSolutionMode == LeverSolutionMode.OrderedSequence)
		{
			HandleOrderedLever(lever, isOn);
			return;
		}
		_activeLeverCount += (isOn ? 1 : (-1));
		_activeLeverCount = Mathf.Clamp(_activeLeverCount, 0, _requiredLeverCount);
		if (AreAllRequiredLeversOn())
		{
			CompletePuzzle();
		}
		else if (_debugLogs)
		{
			Debug.Log($"[WaveRoomPuzzle] Lever changed: {lever.name} = {isOn}. Waiting for all levers.", this);
			LogLeverStates();
		}
	}

	private void HandleOrderedLever(PuzzleLever lever, bool isOn)
	{
		if (!isOn)
		{
			return;
		}
		PuzzleLever[] activeLeverSequence = GetActiveLeverSequence();
		if (activeLeverSequence.Length == 0)
		{
			CompletePuzzle();
			return;
		}
		if (_sequenceIndex < activeLeverSequence.Length && lever == activeLeverSequence[_sequenceIndex])
		{
			_sequenceIndex++;
			PlayCorrect(_sequenceIndex);
			if (_sequenceIndex >= activeLeverSequence.Length)
			{
				CompletePuzzle();
			}
			return;
		}
		PlayWrong();
		if (_spawnEnemyOnMistake)
		{
			SpawnPenaltyEnemy();
		}
		if (_resetSequenceOnMistake)
		{
			ResetLeverSequence();
		}
	}

	private void EnsureSfx()
	{
		if (!(_sfx != null))
		{
			_sfx = GetComponent<AudioSource>();
			if (_sfx == null)
			{
				_sfx = base.gameObject.AddComponent<AudioSource>();
			}
			_sfx.playOnAwake = false;
			_sfx.spatialBlend = 0f;
			_correctClip = ProceduralSfx.Chime(540f, 860f, 0.16f, 0.5f);
			_wrongClip = ProceduralSfx.Chime(300f, 150f, 0.3f, 0.55f);
			_rewardClip = ProceduralSfx.Chime(523f, 1046f, 0.5f, 0.65f);
		}
	}

	private void PlayReward()
	{
		EnsureSfx();
		_sfx.pitch = 1f;
		_sfx.PlayOneShot(_rewardClip);
	}

	private void PlayCorrect(int step)
	{
		EnsureSfx();
		_sfx.pitch = 1f + 0.1f * (float)Mathf.Max(0, step - 1);
		_sfx.PlayOneShot(_correctClip);
	}

	private void PlayWrong()
	{
		EnsureSfx();
		_sfx.pitch = 1f;
		_sfx.PlayOneShot(_wrongClip);
		CameraShake.Shake(0.18f, 0.14f);
		CameraShake.Punch(new Vector3(0f, 0f, 6f));
		PostFXPunch.Punch(0.55f);
	}

	private void SpawnPenaltyEnemy()
	{
		if (_mistakeEnemyPrefab == null || (_maxPenaltyEnemies > 0 && CountAliveEnemies() >= _maxPenaltyEnemies))
		{
			return;
		}
		Transform[] activeSpawnPoints = GetActiveSpawnPoints();
		if (activeSpawnPoints.Length == 0 || !TryChooseWaveSpawnPoint(activeSpawnPoints, null, out var spawnPoint, out var spawnPos))
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(_mistakeEnemyPrefab, spawnPos, spawnPoint.rotation);
		if (!EnsureAgentOnNavMesh(gameObject, spawnPos))
		{
			UnityEngine.Object.Destroy(gameObject);
			return;
		}
		EnemyAI componentInChildren = gameObject.GetComponentInChildren<EnemyAI>();
		if (componentInChildren != null)
		{
			Transform[] patrolWaypoints = ((_autoPatrolArea != null) ? _autoPatrolArea.Waypoints : _enemyPatrolWaypoints);
			componentInChildren.ConfigureForWave(patrolWaypoints, _spawnedEnemyDetectionRange, chasePlayerImmediately: true);
		}
		EnemyHealth componentInChildren2 = gameObject.GetComponentInChildren<EnemyHealth>();
		if (componentInChildren2 != null)
		{
			_aliveEnemies.Add(componentInChildren2);
			componentInChildren2.Died += OnEnemyDied;
		}
		if (_debugLogs)
		{
			Debug.Log("[WaveRoomPuzzle] Wrong lever -> spawned penalty enemy.", this);
		}
	}

	private void CompletePuzzle()
	{
		_completed = true;
		if (_debugLogs)
		{
			Debug.Log("[WaveRoomPuzzle] Puzzle completed. Spawning key and opening doors.", this);
		}
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (puzzleLever != null)
			{
				puzzleLever.SetEnabled(isEnabled: false);
			}
		}
		if (_rewardChest != null)
		{
			_rewardChest.SetActive(value: true);
			RewardReveal component = _rewardChest.GetComponent<RewardReveal>();
			if (component != null)
			{
				component.Reveal();
			}
			else
			{
				PlayReward();
			}
			if (_debugLogs)
			{
				Debug.Log("[WaveRoomPuzzle] Revealed reward chest.", this);
			}
		}
		else if (_keyPrefab != null && _keySpawnPoint != null)
		{
			if (_debugLogs)
			{
				Debug.Log("[WaveRoomPuzzle] Spawning key '" + _keyPrefab.name + "' at '" + _keySpawnPoint.name + "'.", this);
			}
			SpawnKey(GetKeySpawnPosition(_keySpawnPoint.position), _keySpawnPoint.rotation);
			PlayReward();
		}
		else if (_keyPrefab != null)
		{
			Debug.LogWarning("[WaveRoomPuzzle] Key Spawn Point is not assigned. Spawning key at puzzle position.", this);
			SpawnKey(GetKeySpawnPosition(base.transform.position), base.transform.rotation);
			PlayReward();
		}
		else
		{
			Debug.LogWarning("[WaveRoomPuzzle] Key Prefab is not assigned. Puzzle completed, but no key was spawned.", this);
		}
		OpenRewardDoors();
	}

	private bool AreAllRequiredLeversOn()
	{
		bool result = false;
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (!(puzzleLever == null))
			{
				result = true;
				if (!puzzleLever.IsOn)
				{
					return false;
				}
			}
		}
		return result;
	}

	private void LogLeverStates()
	{
		if (_levers != null)
		{
			for (int i = 0; i < _levers.Length; i++)
			{
				PuzzleLever puzzleLever = _levers[i];
				Debug.Log(string.Format("[WaveRoomPuzzle] Lever {0}: {1} = {2}", i, (puzzleLever == null) ? "NULL" : puzzleLever.name, puzzleLever != null && puzzleLever.IsOn), this);
			}
		}
	}

	private void SpawnKey(Vector3 position, Quaternion rotation)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(_keyPrefab, position, rotation);
		gameObject.SetActive(value: true);
		SetupKeyPickup(gameObject);
	}

	private Vector3 GetKeySpawnPosition(Vector3 basePosition)
	{
		return basePosition + Vector3.up * _keySpawnHeightOffset;
	}

	private void SetupKeyPickup(GameObject key)
	{
		if (key == null)
		{
			return;
		}
		KeyPickup keyPickup = key.GetComponentInChildren<KeyPickup>();
		if (keyPickup == null)
		{
			keyPickup = key.AddComponent<KeyPickup>();
		}
		keyPickup.SetPickupDelay(_keyPickupDelay);
		keyPickup.SetPickupOnTrigger(pickupOnTrigger: true);
		Collider[] componentsInChildren = key.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider != null)
			{
				collider.enabled = false;
			}
		}
		SphereCollider sphereCollider = key.GetComponent<SphereCollider>();
		if (sphereCollider == null)
		{
			sphereCollider = key.AddComponent<SphereCollider>();
		}
		sphereCollider.enabled = true;
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 1.25f;
		sphereCollider.center = Vector3.zero;
		Rigidbody rigidbody = key.GetComponent<Rigidbody>();
		if (rigidbody == null)
		{
			rigidbody = key.AddComponent<Rigidbody>();
		}
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
	}

	private PuzzleLever[] GetActiveLeverSequence()
	{
		if (_leverSequence != null && _leverSequence.Length != 0)
		{
			return _leverSequence;
		}
		return _levers;
	}

	private void ResetLeverSequence()
	{
		_resettingLevers = true;
		_sequenceIndex = 0;
		PuzzleLever[] levers = _levers;
		foreach (PuzzleLever puzzleLever in levers)
		{
			if (puzzleLever != null)
			{
				puzzleLever.SetOn(isOn: false, notify: false);
			}
		}
		_resettingLevers = false;
	}

	private void OpenRewardDoors()
	{
		if (_openRoomDoorOnComplete && _roomDoor != null)
		{
			_roomDoor.SetLocked(isLocked: false);
			_roomDoor.Open();
		}
		if (_openRoomDoorOnComplete && _lockedRoomDoor != null)
		{
			_lockedRoomDoor.Unseal(open: true);
		}
		if (_doorsToOpenOnComplete == null)
		{
			return;
		}
		Door[] doorsToOpenOnComplete = _doorsToOpenOnComplete;
		foreach (Door door in doorsToOpenOnComplete)
		{
			if (!(door == null))
			{
				door.SetLocked(isLocked: false);
				door.Open();
			}
		}
	}

	private void CloseAndLockDoor()
	{
		if (_roomDoor != null)
		{
			_roomDoor.Close();
			_roomDoor.SetLocked(isLocked: true);
		}
		if (_lockedRoomDoor != null)
		{
			_lockedRoomDoor.Seal();
		}
	}
}
