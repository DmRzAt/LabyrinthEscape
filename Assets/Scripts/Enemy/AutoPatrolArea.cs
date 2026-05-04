using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BoxCollider))]
public class AutoPatrolArea : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField, Range(2, 32)] private int _waypointCount = 6;
    [SerializeField, Range(1, 16)] private int _spawnPointCount = 3;
    [SerializeField] private float _navMeshSampleRadius = 2f;
    [SerializeField] private float _minSpacing = 1.5f;
    [SerializeField, Range(0f, 0.45f)] private float _edgePadding = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    private Transform[] _waypoints;
    private Transform[] _spawnPoints;
    private bool _generated;

    public Transform[] Waypoints { get { EnsureGenerated(); return _waypoints; } }
    public Transform[] SpawnPoints { get { EnsureGenerated(); return _spawnPoints; } }

    private void Awake() => EnsureGenerated();

    private void EnsureGenerated()
    {
        if (_generated) return;
        _generated = true;

        var box = GetComponent<BoxCollider>();
        var taken = new List<Vector3>();

        _waypoints = GeneratePoints(box, _waypointCount, "WP_", taken);
        _spawnPoints = GeneratePoints(box, _spawnPointCount, "SP_", taken);
    }

    private Transform[] GeneratePoints(BoxCollider box, int count, string prefix, List<Vector3> taken)
    {
        var list = new List<Transform>(count);
        float minSpacingSqr = _minSpacing * _minSpacing;
        List<Vector3> candidates = BuildFixedGridCandidates(box, count);

        foreach (Vector3 local in candidates)
        {
            if (list.Count >= count) break;

            Vector3 world = transform.TransformPoint(local);

            if (!NavMesh.SamplePosition(world, out var hit, _navMeshSampleRadius, NavMesh.AllAreas))
                continue;

            bool tooClose = false;
            foreach (Vector3 usedPosition in taken)
            {
                if ((usedPosition - hit.position).sqrMagnitude < minSpacingSqr)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            var go = new GameObject(prefix + list.Count);
            go.transform.SetParent(transform);
            go.transform.position = hit.position;
            list.Add(go.transform);
            taken.Add(hit.position);
        }

        return list.ToArray();
    }

    private List<Vector3> BuildFixedGridCandidates(BoxCollider box, int targetCount)
    {
        Vector3 center = box.center;
        Vector3 size = box.size;
        int gridSide = Mathf.CeilToInt(Mathf.Sqrt(targetCount * 4f));
        var candidates = new List<Vector3>(gridSide * gridSide);

        for (int x = 0; x < gridSide; x++)
        {
            for (int z = 0; z < gridSide; z++)
            {
                float xT = gridSide == 1 ? 0.5f : (float)x / (gridSide - 1);
                float zT = gridSide == 1 ? 0.5f : (float)z / (gridSide - 1);

                xT = Mathf.Lerp(_edgePadding, 1f - _edgePadding, xT);
                zT = Mathf.Lerp(_edgePadding, 1f - _edgePadding, zT);

                Vector3 local = new Vector3(
                    center.x + Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, xT),
                    center.y,
                    center.z + Mathf.Lerp(-size.z * 0.5f, size.z * 0.5f, zT));

                candidates.Add(local);
            }
        }

        candidates.Sort((left, right) =>
        {
            float leftDistance = (left - center).sqrMagnitude;
            float rightDistance = (right - center).sqrMagnitude;
            return rightDistance.CompareTo(leftDistance);
        });

        return candidates;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
