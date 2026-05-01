using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    public enum HingeSide { Left, Right }

    [Header("Налаштування")]
    public int keysRequired = 1;
    public float openAngle = 90f;
    public float speed = 2f;

    [Tooltip("Перетягни сюди стулку (UnlockedLeaf)")]
    public Transform doorLeaf;

    [Tooltip("З якого боку петлі відносно стулки")]
    public HingeSide hingeSide = HingeSide.Left;

    private bool _unlocked = false;
    private bool _open = false;
    private Transform _hinge;
    private Quaternion _closedRot;
    private Quaternion _openRot;

    public string Prompt => _unlocked ? "Open Door" : $"Locked  (Keys: {keysRequired})";

    void Start()
    {
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
            if (GameManager.Instance.keysCollected >= keysRequired)
            {
                _unlocked = true;
                _open = true;
            }
        }
        else
        {
            _open = !_open;
        }
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
