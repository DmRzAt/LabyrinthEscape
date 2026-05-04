using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveRoomPuzzle : MonoBehaviour
{
    private enum LeverSolutionMode { AnyOrderAllOn, OrderedSequence }

    [System.Serializable]
    public class EnemyWave
    {
        [SerializeField] private GameObject[] _enemyPrefabs;

        public GameObject[] EnemyPrefabs => _enemyPrefabs;
    }

    [Header("Room")]
    [SerializeField] private Door _roomDoor;
    [SerializeField] private GameObject _floorSymbol;

    [Header("Waves")]
    [SerializeField] private EnemyWave[] _waves = new EnemyWave[2];
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField, Range(0f, 5f)] private float _spawnDelay = 0.25f;
    [SerializeField] private float _navMeshSampleRadius = 2f;

    [Header("Spawned Enemy AI")]
    [SerializeField] private Transform[] _enemyPatrolWaypoints;
    [SerializeField, Range(1f, 50f)] private float _spawnedEnemyDetectionRange = 18f;
    [SerializeField] private bool _chasePlayerImmediately = true;

    [Header("Auto (BoxCollider area)")]
    [SerializeField] private AutoPatrolArea _autoPatrolArea;

    [Header("Levers")]
    [SerializeField] private GameObject _leversRoot;
    [SerializeField] private PuzzleLever[] _levers = new PuzzleLever[4];
    [SerializeField] private LeverSolutionMode _leverSolutionMode = LeverSolutionMode.AnyOrderAllOn;
    [SerializeField] private PuzzleLever[] _leverSequence;
    [SerializeField] private bool _resetSequenceOnMistake = true;

    [Header("Reward")]
    [SerializeField] private GameObject _keyPrefab;
    [SerializeField] private Transform _keySpawnPoint;
    [SerializeField, Range(0f, 3f)] private float _keySpawnHeightOffset = 1f;
    [SerializeField, Range(0f, 3f)] private float _keyPickupDelay = 0.75f;
    [SerializeField] private Door[] _doorsToOpenOnComplete;
    [SerializeField] private bool _openRoomDoorOnComplete = true;
    [SerializeField] private bool _debugLogs = true;

    private readonly List<EnemyHealth> _aliveEnemies = new List<EnemyHealth>();
    private int _activeLeverCount;
    private int _requiredLeverCount;
    private int _sequenceIndex;
    private bool _started;
    private bool _completed;
    private bool _resettingLevers;

    private void Awake()
    {
        if (_floorSymbol != null) _floorSymbol.SetActive(false);
        if (_leversRoot != null) _leversRoot.SetActive(false);

        foreach (PuzzleLever lever in _levers)
        {
            if (lever == null) continue;

            _requiredLeverCount++;
            lever.SetOn(false, false);
            lever.SetEnabled(false);
            lever.StateChanged += OnLeverStateChanged;
        }
    }

    private void OnDestroy()
    {
        foreach (PuzzleLever lever in _levers)
        {
            if (lever != null)
            {
                lever.StateChanged -= OnLeverStateChanged;
            }
        }

        foreach (EnemyHealth enemy in _aliveEnemies)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_started || !other.CompareTag("Player")) return;

        _started = true;
        CloseAndLockDoor();
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        for (int waveIndex = 0; waveIndex < _waves.Length; waveIndex++)
        {
            yield return SpawnWave(_waves[waveIndex]);
            yield return new WaitUntil(() => _aliveEnemies.Count == 0);
        }

        RevealLeverPuzzle();
    }

    private IEnumerator SpawnWave(EnemyWave wave)
    {
        Transform[] activeSpawnPoints = GetActiveSpawnPoints();
        if (wave == null || wave.EnemyPrefabs == null || wave.EnemyPrefabs.Length == 0 || activeSpawnPoints.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < activeSpawnPoints.Length; i++)
        {
            GameObject enemyPrefab = wave.EnemyPrefabs[i % wave.EnemyPrefabs.Length];
            Transform spawnPoint = activeSpawnPoints[i];

            if (enemyPrefab != null && spawnPoint != null && TryGetNavMeshPosition(spawnPoint.position, out Vector3 spawnPos))
            {
                GameObject enemyObject = Instantiate(enemyPrefab, spawnPos, spawnPoint.rotation);
                EnemyAI enemyAI = enemyObject.GetComponentInChildren<EnemyAI>();
                if (enemyAI != null)
                {
                    Transform[] patrol = _autoPatrolArea != null ? _autoPatrolArea.Waypoints : _enemyPatrolWaypoints;
                    enemyAI.ConfigureForWave(patrol, _spawnedEnemyDetectionRange, _chasePlayerImmediately);
                }

                EnemyHealth enemyHealth = enemyObject.GetComponentInChildren<EnemyHealth>();

                if (enemyHealth != null)
                {
                    _aliveEnemies.Add(enemyHealth);
                    enemyHealth.Died += OnEnemyDied;
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
        if (_autoPatrolArea != null && _autoPatrolArea.SpawnPoints != null && _autoPatrolArea.SpawnPoints.Length > 0)
        {
            return _autoPatrolArea.SpawnPoints;
        }

        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            return _spawnPoints;
        }

        return System.Array.Empty<Transform>();
    }

    private bool TryGetNavMeshPosition(Vector3 source, out Vector3 result)
    {
        if (NavMesh.SamplePosition(source, out var hit, _navMeshSampleRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        Debug.LogWarning($"[WaveRoomPuzzle] No NavMesh near {source}, skipping spawn.", this);
        result = source;
        return false;
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        if (enemy == null) return;

        enemy.Died -= OnEnemyDied;
        _aliveEnemies.Remove(enemy);
    }

    private void RevealLeverPuzzle()
    {
        if (_floorSymbol != null) _floorSymbol.SetActive(true);
        if (_leversRoot != null) _leversRoot.SetActive(true);

        _activeLeverCount = 0;
        _sequenceIndex = 0;

        foreach (PuzzleLever lever in _levers)
        {
            if (lever != null)
            {
                lever.SetOn(false, false);
                lever.SetEnabled(true);
            }
        }

        if (_requiredLeverCount == 0)
        {
            CompletePuzzle();
        }
    }

    private void OnLeverStateChanged(PuzzleLever lever, bool isOn)
    {
        if (_completed || _resettingLevers) return;

        if (_leverSolutionMode == LeverSolutionMode.OrderedSequence)
        {
            HandleOrderedLever(lever, isOn);
            return;
        }

        _activeLeverCount += isOn ? 1 : -1;
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
        if (!isOn) return;

        PuzzleLever[] sequence = GetActiveLeverSequence();
        if (sequence.Length == 0)
        {
            CompletePuzzle();
            return;
        }

        if (_sequenceIndex < sequence.Length && lever == sequence[_sequenceIndex])
        {
            _sequenceIndex++;
            if (_sequenceIndex >= sequence.Length)
            {
                CompletePuzzle();
            }

            return;
        }

        if (_resetSequenceOnMistake)
        {
            ResetLeverSequence();
        }
    }

    private void CompletePuzzle()
    {
        _completed = true;

        if (_debugLogs)
        {
            Debug.Log("[WaveRoomPuzzle] Puzzle completed. Spawning key and opening doors.", this);
        }

        foreach (PuzzleLever lever in _levers)
        {
            if (lever != null)
            {
                lever.SetEnabled(false);
            }
        }

        if (_keyPrefab != null && _keySpawnPoint != null)
        {
            if (_debugLogs)
            {
                Debug.Log($"[WaveRoomPuzzle] Spawning key '{_keyPrefab.name}' at '{_keySpawnPoint.name}'.", this);
            }

            SpawnKey(GetKeySpawnPosition(_keySpawnPoint.position), _keySpawnPoint.rotation);
        }
        else if (_keyPrefab != null)
        {
            Debug.LogWarning("[WaveRoomPuzzle] Key Spawn Point is not assigned. Spawning key at puzzle position.", this);
            SpawnKey(GetKeySpawnPosition(transform.position), transform.rotation);
        }
        else
        {
            Debug.LogWarning("[WaveRoomPuzzle] Key Prefab is not assigned. Puzzle completed, but no key was spawned.", this);
        }

        OpenRewardDoors();
    }

    private bool AreAllRequiredLeversOn()
    {
        bool hasLever = false;

        foreach (PuzzleLever lever in _levers)
        {
            if (lever == null) continue;

            hasLever = true;
            if (!lever.IsOn)
            {
                return false;
            }
        }

        return hasLever;
    }

    private void LogLeverStates()
    {
        if (_levers == null) return;

        for (int i = 0; i < _levers.Length; i++)
        {
            PuzzleLever lever = _levers[i];
            Debug.Log($"[WaveRoomPuzzle] Lever {i}: {(lever == null ? "NULL" : lever.name)} = {(lever != null && lever.IsOn)}", this);
        }
    }

    private void SpawnKey(Vector3 position, Quaternion rotation)
    {
        GameObject key = Instantiate(_keyPrefab, position, rotation);
        key.SetActive(true);
        SetupKeyPickup(key);
    }

    private Vector3 GetKeySpawnPosition(Vector3 basePosition)
    {
        return basePosition + Vector3.up * _keySpawnHeightOffset;
    }

    private void SetupKeyPickup(GameObject key)
    {
        if (key == null) return;

        KeyPickup keyPickup = key.GetComponentInChildren<KeyPickup>();
        if (keyPickup == null)
        {
            keyPickup = key.AddComponent<KeyPickup>();
        }

        keyPickup.SetPickupDelay(_keyPickupDelay);
        keyPickup.SetPickupOnTrigger(false);

        Collider[] existingColliders = key.GetComponentsInChildren<Collider>();
        foreach (Collider existingCollider in existingColliders)
        {
            if (existingCollider != null)
            {
                existingCollider.enabled = false;
            }
        }

        SphereCollider pickupCollider = key.GetComponent<SphereCollider>();
        if (pickupCollider == null)
        {
            pickupCollider = key.AddComponent<SphereCollider>();
        }

        pickupCollider.enabled = true;
        pickupCollider.isTrigger = true;
        pickupCollider.radius = 1.25f;
        pickupCollider.center = Vector3.zero;

        Rigidbody keyRigidbody = key.GetComponent<Rigidbody>();
        if (keyRigidbody == null)
        {
            keyRigidbody = key.AddComponent<Rigidbody>();
        }

        keyRigidbody.isKinematic = true;
        keyRigidbody.useGravity = false;
    }

    private PuzzleLever[] GetActiveLeverSequence()
    {
        if (_leverSequence != null && _leverSequence.Length > 0)
        {
            return _leverSequence;
        }

        return _levers;
    }

    private void ResetLeverSequence()
    {
        _resettingLevers = true;
        _sequenceIndex = 0;

        foreach (PuzzleLever lever in _levers)
        {
            if (lever != null)
            {
                lever.SetOn(false, false);
            }
        }

        _resettingLevers = false;
    }

    private void OpenRewardDoors()
    {
        if (_openRoomDoorOnComplete && _roomDoor != null)
        {
            _roomDoor.SetLocked(false);
            _roomDoor.Open();
        }

        if (_doorsToOpenOnComplete == null) return;

        foreach (Door door in _doorsToOpenOnComplete)
        {
            if (door == null) continue;

            door.SetLocked(false);
            door.Open();
        }
    }

    private void CloseAndLockDoor()
    {
        if (_roomDoor == null) return;

        _roomDoor.Close();
        _roomDoor.SetLocked(true);
    }
}
