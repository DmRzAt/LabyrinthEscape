using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Налаштування")]
    public int keysRequired = 1;
    public float openAngle = 90f;
    public float speed = 2f;

    [Header("Об'єкти")]
    public Transform doorTransform;
    public Transform pivotPoint;

    private bool _unlocked = false;
    private bool _open = false;
    private Quaternion _closedRot;
    private Quaternion _openRot;
    private Transform _rotTarget;

    public string Prompt => _unlocked ? "Open Door" : $"Locked  (Keys: {keysRequired})";

    void Start()
    {
        Transform door = doorTransform != null ? doorTransform : transform;

        if (pivotPoint != null)
        {
            _rotTarget = pivotPoint;
            if (door.parent != pivotPoint)
                door.SetParent(pivotPoint, true);
        }
        else
        {
            _rotTarget = door;
        }

        _closedRot = _rotTarget.rotation;
        _openRot = _closedRot * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        _rotTarget.rotation = Quaternion.Slerp(_rotTarget.rotation,
            _open ? _openRot : _closedRot, Time.deltaTime * speed);
    }

    public void Interact()
    {
        if (!_unlocked)
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.keysCollected >= keysRequired)
            {
                for (int i = 0; i < keysRequired; i++) GameManager.Instance.UseKey();
                _unlocked = true;
                _open = true;
            }
        }
        else
        {
            _open = !_open;
        }
    }
}
