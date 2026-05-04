using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CorridorEnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private class PatrolZone
    {
        [SerializeField] private string _name = "Patrol Zone";
        [SerializeField] private AutoPatrolArea _autoPatrolArea;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private Transform[] _patrolWaypoints;

        public string Name => _name;
        public Transform[] SpawnPoints => _autoPatrolArea != null ? _autoPatrolArea.SpawnPoints : _spawnPoints;
        public Transform[] PatrolWaypoints => _autoPatrolArea != null ? _autoPatrolArea.Waypoints : _patrolWaypoints;
        public bool HasSpawnPoints => SpawnPoints != null && SpawnPoints.Length > 0;
    }

    [Header("Spawn")]
    [SerializeField] private GameObject[] _enemyPrefabs;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField, Range(0f, 5f)] private float _spawnDelay = 0.25f;
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private float _navMeshSampleRadius = 2f;

    [Header("Patrol")]
    [SerializeField] private Transform[] _patrolWaypoints;
    [SerializeField, Range(1f, 50f)] private float _detectionRange = 6f;
    [SerializeField] private bool _chasePlayerImmediately = false;

    [Header("Patrol Zones")]
    [SerializeField] private PatrolZone[] _patrolZones;

    [Header("Auto (BoxCollider area)")]
    [SerializeField] private AutoPatrolArea _autoPatrolArea;
    [SerializeField, Range(0, 16)] private int _autoEnemyCount = 0;

    private bool _spawned;

    private void Start()
    {
        if (_spawnOnStart) Spawn();
    }

    public void Spawn()
    {
        if (_spawned || _enemyPrefabs == null || _enemyPrefabs.Length == 0) return;
        if (!HasAnySpawnPoints() && _autoPatrolArea == null) return;

        _spawned = true;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        if (_autoPatrolArea != null && _autoEnemyCount > 0)
        {
            yield return SpawnAutoEnemies();
            yield break;
        }

        if (HasPatrolZones())
        {
            yield return SpawnZoneEnemies();
            yield break;
        }

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            SpawnEnemy(_spawnPoints[i], _patrolWaypoints, i);
            if (_spawnDelay > 0f) yield return new WaitForSeconds(_spawnDelay);
        }
    }

    private IEnumerator SpawnAutoEnemies()
    {
        Transform[] spawn = _autoPatrolArea.SpawnPoints;
        Transform[] waypoints = _autoPatrolArea.Waypoints;
        if (spawn == null || spawn.Length == 0) yield break;

        for (int i = 0; i < _autoEnemyCount; i++)
        {
            SpawnEnemy(spawn[i % spawn.Length], waypoints, i);
            if (_spawnDelay > 0f) yield return new WaitForSeconds(_spawnDelay);
        }
    }

    private IEnumerator SpawnZoneEnemies()
    {
        int spawnedCount = 0;

        foreach (PatrolZone zone in _patrolZones)
        {
            if (zone == null || !zone.HasSpawnPoints) continue;

            Transform[] zoneSpawnPoints = zone.SpawnPoints;
            for (int i = 0; i < zoneSpawnPoints.Length; i++)
            {
                SpawnEnemy(zoneSpawnPoints[i], zone.PatrolWaypoints, spawnedCount);
                spawnedCount++;
                if (_spawnDelay > 0f) yield return new WaitForSeconds(_spawnDelay);
            }
        }
    }

    private void SpawnEnemy(Transform spawnPoint, Transform[] patrolWaypoints, int prefabIndex)
    {
        if (spawnPoint == null) return;

        GameObject enemyPrefab = _enemyPrefabs[prefabIndex % _enemyPrefabs.Length];
        if (enemyPrefab == null) return;

        if (!TryGetNavMeshPosition(spawnPoint.position, out Vector3 spawnPos)) return;

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPos, spawnPoint.rotation);
        EnemyAI enemyAI = enemyObject.GetComponentInChildren<EnemyAI>();

        if (enemyAI != null)
            enemyAI.ConfigureForWave(patrolWaypoints, _detectionRange, _chasePlayerImmediately);
    }

    private bool TryGetNavMeshPosition(Vector3 source, out Vector3 result)
    {
        if (NavMesh.SamplePosition(source, out var hit, _navMeshSampleRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        Debug.LogWarning($"[CorridorEnemySpawner] No NavMesh near {source}, skipping spawn.", this);
        result = source;
        return false;
    }

    private bool HasAnySpawnPoints()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0) return true;
        return HasPatrolZones();
    }

    private bool HasPatrolZones()
    {
        if (_patrolZones == null || _patrolZones.Length == 0) return false;
        foreach (PatrolZone zone in _patrolZones)
            if (zone != null && zone.HasSpawnPoints) return true;
        return false;
    }
}
