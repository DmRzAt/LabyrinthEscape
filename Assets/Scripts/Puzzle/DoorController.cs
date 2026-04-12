using UnityEngine;

/// <summary>
/// Drzwi w labiryncie. Mogą wymagać kluczy lub przełącznika.
/// Po otwarciu — animacja przesunięcia/obrotu.
/// </summary>
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorType { KeyDoor, SwitchDoor, ExitDoor }

    [Header("Door Settings")]
    [SerializeField] private DoorType doorType = DoorType.KeyDoor;
    [SerializeField] private bool requiresAllKeys = false;
    [SerializeField] private int keysRequired = 1;

    [Header("Animation")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f); // przesunięcie do góry
    [SerializeField] private float openSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip lockedSound;

    private bool isOpen = false;
    private bool isMoving = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    private void Update()
    {
        if (isMoving)
        {
            Vector3 target = isOpen ? openPosition : closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                transform.position = target;
                isMoving = false;
            }
        }
    }

    public void Interact()
    {
        if (isOpen || isMoving) return;

        switch (doorType)
        {
            case DoorType.KeyDoor:
                TryOpenWithKeys();
                break;
            case DoorType.ExitDoor:
                TryOpenExitDoor();
                break;
            case DoorType.SwitchDoor:
                // Ці двері відкриваються тільки перемикачем
                PlaySound(lockedSound);
                Debug.Log("[Door] This door requires a switch!");
                break;
        }
    }

    /// <summary>
    /// Відкриває двері ключами.
    /// </summary>
    private void TryOpenWithKeys()
    {
        if (GameManager.Instance == null) return;

        bool canOpen = requiresAllKeys
            ? GameManager.Instance.HasAllKeys()
            : GameManager.Instance.GetKeysCollected() >= keysRequired;

        if (canOpen)
        {
            OpenDoor();
        }
        else
        {
            PlaySound(lockedSound);
            Debug.Log($"[Door] Locked! Need {(requiresAllKeys ? "all" : keysRequired.ToString())} keys.");
        }
    }

    /// <summary>
    /// Двері виходу — потрібні всі ключі → перехід на EndScene.
    /// </summary>
    private void TryOpenExitDoor()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.HasAllKeys())
        {
            OpenDoor();
            GameManager.Instance.LevelCompleted();
            // Затримка перед переходом на EndScene
            Invoke(nameof(GoToEndScene), 2f);
        }
        else
        {
            PlaySound(lockedSound);
            Debug.Log("[Door] EXIT locked! Collect all keys first.");
        }
    }

    /// <summary>
    /// Викликається SwitchController для відкриття SwitchDoor.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;

        isOpen = true;
        isMoving = true;
        PlaySound(openSound);
        Debug.Log($"[Door] {gameObject.name} opened!");
    }

    public void CloseDoor()
    {
        if (!isOpen) return;

        isOpen = false;
        isMoving = true;
    }

    private void GoToEndScene()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadEndScene();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
