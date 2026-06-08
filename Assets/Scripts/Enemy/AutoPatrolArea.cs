using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BoxCollider))]
public class AutoPatrolArea : MonoBehaviour
{
	[Header("Generation")]
	[SerializeField]
	[Range(2f, 32f)]
	private int _waypointCount = 6;

	[SerializeField]
	[Range(1f, 16f)]
	private int _spawnPointCount = 3;

	[SerializeField]
	private float _navMeshSampleRadius = 2f;

	[SerializeField]
	private float _minSpacing = 1.5f;

	[SerializeField]
	[Range(0f, 0.45f)]
	private float _edgePadding = 0.15f;

	[Header("Debug")]
	[SerializeField]
	private bool _drawGizmos = true;

	private Transform[] _waypoints;

	private Transform[] _spawnPoints;

	private bool _generated;

	public Transform[] Waypoints
	{
		get
		{
			EnsureGenerated();
			return _waypoints;
		}
	}

	public Transform[] SpawnPoints
	{
		get
		{
			EnsureGenerated();
			return _spawnPoints;
		}
	}

	private void Awake()
	{
		EnsureGenerated();
	}

	private void EnsureGenerated()
	{
		if (_generated)
		{
			return;
		}
		_generated = true;
		BoxCollider component = GetComponent<BoxCollider>();
		List<Vector3> list = new List<Vector3>();
		float num = _minSpacing * _minSpacing;
		foreach (Vector3 item in BuildFixedGridCandidates(component, (_waypointCount + _spawnPointCount) * 3))
		{
			if (!NavMesh.SamplePosition(base.transform.TransformPoint(item), out var hit, _navMeshSampleRadius, -1))
			{
				continue;
			}
			bool flag = false;
			foreach (Vector3 item2 in list)
			{
				if ((item2 - hit.position).sqrMagnitude < num)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(hit.position);
			}
		}
		List<Vector3> list2 = LargestConnectedCluster(list);
		int count = list2.Count;
		int num2 = Mathf.Min(_spawnPointCount, count);
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < num2; i++)
		{
			hashSet.Add((num2 <= 1) ? (count / 2) : Mathf.RoundToInt((float)i * (float)(count - 1) / (float)(num2 - 1)));
		}
		List<Vector3> list3 = new List<Vector3>();
		List<Vector3> list4 = new List<Vector3>();
		for (int j = 0; j < count; j++)
		{
			if (hashSet.Contains(j))
			{
				list3.Add(list2[j]);
			}
			else if (list4.Count < _waypointCount)
			{
				list4.Add(list2[j]);
			}
		}
		_spawnPoints = MakePoints(list3, "SP_");
		_waypoints = MakePoints(list4, "WP_");
	}

	private Transform[] MakePoints(List<Vector3> positions, string prefix)
	{
		List<Transform> list = new List<Transform>(positions.Count);
		for (int i = 0; i < positions.Count; i++)
		{
			GameObject gameObject = new GameObject(prefix + i);
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.position = positions[i];
			list.Add(gameObject.transform);
		}
		return list.ToArray();
	}

	private List<Vector3> LargestConnectedCluster(List<Vector3> points)
	{
		if (points.Count == 0)
		{
			return points;
		}
		List<List<Vector3>> list = new List<List<Vector3>>();
		NavMeshPath navMeshPath = new NavMeshPath();
		foreach (Vector3 point in points)
		{
			bool flag = false;
			foreach (List<Vector3> item in list)
			{
				NavMesh.CalculatePath(item[0], point, -1, navMeshPath);
				if (navMeshPath.status == NavMeshPathStatus.PathComplete)
				{
					item.Add(point);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(new List<Vector3> { point });
			}
		}
		List<Vector3> result = points;
		int num = 0;
		foreach (List<Vector3> item2 in list)
		{
			if (item2.Count > num)
			{
				num = item2.Count;
				result = item2;
			}
		}
		return result;
	}

	private List<Vector3> BuildFixedGridCandidates(BoxCollider box, int targetCount)
	{
		Vector3 center = box.center;
		Vector3 size = box.size;
		int num = Mathf.CeilToInt(Mathf.Sqrt((float)targetCount * 4f));
		List<Vector3> list = new List<Vector3>(num * num);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float t = ((num == 1) ? 0.5f : ((float)i / (float)(num - 1)));
				float t2 = ((num == 1) ? 0.5f : ((float)j / (float)(num - 1)));
				t = Mathf.Lerp(_edgePadding, 1f - _edgePadding, t);
				t2 = Mathf.Lerp(_edgePadding, 1f - _edgePadding, t2);
				Vector3 item = new Vector3(center.x + Mathf.Lerp((0f - size.x) * 0.5f, size.x * 0.5f, t), center.y, center.z + Mathf.Lerp((0f - size.z) * 0.5f, size.z * 0.5f, t2));
				list.Add(item);
			}
		}
		list.Sort(delegate(Vector3 left, Vector3 right)
		{
			float sqrMagnitude = (left - center).sqrMagnitude;
			return (right - center).sqrMagnitude.CompareTo(sqrMagnitude);
		});
		return list;
	}

	private void OnDrawGizmosSelected()
	{
		if (_drawGizmos)
		{
			BoxCollider component = GetComponent<BoxCollider>();
			if (!(component == null))
			{
				Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
				Gizmos.matrix = base.transform.localToWorldMatrix;
				Gizmos.DrawCube(component.center, component.size);
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireCube(component.center, component.size);
			}
		}
	}
}
