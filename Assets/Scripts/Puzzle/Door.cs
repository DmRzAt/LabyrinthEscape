using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Налаштування")]
    public float openAngle = 90f;
    public float speed = 3f;
    public bool locked = false;
    public string prompt = "Open Door";

    [Tooltip("Перетягни сюди UnlockedLeaf — тільки він буде крутитись")]
    public Transform doorLeaf;

    public string Prompt => locked ? "Locked" : prompt;

    private Quaternion _closedRot;
    private Quaternion _openRot;
    private bool _isOpen = false;

    void Start()
    {
        if (doorLeaf == null) doorLeaf = transform;
        _closedRot = doorLeaf.localRotation;
        _openRot = _closedRot * Quaternion.Euler(0, openAngle, 0);
    }

    void Update()
    {
        doorLeaf.localRotation = Quaternion.Slerp(
            doorLeaf.localRotation,
            _isOpen ? _openRot : _closedRot,
            Time.deltaTime * speed
        );
    }

    public void Interact()
    {
        if (locked) return;
        _isOpen = !_isOpen;
    }

    public void Unlock() => locked = false;
}
