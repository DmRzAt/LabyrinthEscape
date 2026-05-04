using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    public enum HingeSide { Left, Right }

    [Header("Settings")]
    public int keysRequired = 1;
    public float openAngle = 90f;
    public float speed = 2f;
    [SerializeField] private bool _debugLogs = true;

    [Tooltip("Assign the door leaf, for example UnlockedLeaf.")]
    public Transform doorLeaf;

    [Tooltip("Hinge side relative to the door leaf.")]
    public HingeSide hingeSide = HingeSide.Left;

    private bool _unlocked = false;
    private bool _open = false;
    private bool _syncingLinkedDoors;
    private Transform _hinge;
    private Quaternion _closedRot;
    private Quaternion _openRot;

    public string Prompt => _unlocked ? "Open Door" : $"Locked  (Need: {RequiredKeys}, Have: {AvailableKeys})";

    private int RequiredKeys => Mathf.Max(1, keysRequired);
    private int AvailableKeys => GameManager.Instance != null ? GameManager.Instance.keysAvailable : 0;

    private void OnValidate()
    {
        keysRequired = Mathf.Max(1, keysRequired);
    }

    void Start()
    {
        keysRequired = RequiredKeys;

        if (doorLeaf == null) doorLeaf = transform;

        _hinge = new GameObject(doorLeaf.name + "_Hinge").transform;
        _hinge.SetParent(doorLeaf.parent, false);

        _hinge.position = GetHingeEdgeWorld(doorLeaf, hingeSide);
        _hinge.rotation = doorLeaf.rotation;

        doorLeaf.SetParent(_hinge, true);

        _closedRot = _hinge.localRotation;
        _openRot = _closedRot * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        if (_hinge == null) return;
        _hinge.localRotation = Quaternion.Slerp(_hinge.localRotation,
            _open ? _openRot : _closedRot, Time.deltaTime * speed);
    }

    public void Interact()
    {
        if (!_unlocked)
        {
            if (GameManager.Instance == null) return;

            if (_debugLogs)
            {
                Debug.Log($"[LockedDoor] Interact '{name}'. Need={RequiredKeys}, available={GameManager.Instance.keysAvailable}, collected={GameManager.Instance.keysCollected}", this);
            }

            if (AvailableKeys < RequiredKeys)
            {
                return;
            }

            for (int i = 0; i < RequiredKeys; i++)
            {
                if (!GameManager.Instance.UseKey())
                {
                    return;
                }
            }

            UnlockAndOpenLinkedDoors();
            return;
        }

        _open = !_open;
    }

    private void UnlockAndOpenLinkedDoors()
    {
        SetUnlockedOpen(true);

        if (_syncingLinkedDoors) return;

        _syncingLinkedDoors = true;
        SyncLinkedDoors(GetComponentsInParent<LockedDoor>(true));
        SyncLinkedDoors(GetComponentsInChildren<LockedDoor>(true));
        _syncingLinkedDoors = false;
    }

    private void SyncLinkedDoors(LockedDoor[] linkedDoors)
    {
        if (linkedDoors == null) return;

        foreach (LockedDoor linkedDoor in linkedDoors)
        {
            if (linkedDoor == null || linkedDoor == this) continue;

            linkedDoor.SetUnlockedOpen(false);
        }
    }

    private void SetUnlockedOpen(bool open)
    {
        _unlocked = true;
        _open = open;
    }

    private static Vector3 GetHingeEdgeWorld(Transform leaf, HingeSide side)
    {
        var renderers = leaf.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return leaf.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        Vector3 right = leaf.right;
        Vector3 center = b.center;
        float halfExtent = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z)));
        return center + right * (side == HingeSide.Left ? -halfExtent : halfExtent);
    }
}
