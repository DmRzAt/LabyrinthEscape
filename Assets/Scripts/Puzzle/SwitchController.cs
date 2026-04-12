using UnityEngine;

/// <summary>
/// Przełącznik (switch/lever) — po interakcji otwiera powiązane drzwi.
/// </summary>
public class SwitchController : MonoBehaviour, IInteractable
{
    [Header("Linked Doors")]
    [SerializeField] private DoorController[] linkedDoors;

    [Header("Visual")]
    [SerializeField] private Material activatedMaterial;
    [SerializeField] private Vector3 activatedRotation = new Vector3(-45f, 0f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip switchSound;

    private bool isActivated = false;
    private Renderer switchRenderer;
    private Material originalMaterial;

    private void Start()
    {
        switchRenderer = GetComponent<Renderer>();
        if (switchRenderer != null)
            originalMaterial = switchRenderer.material;
    }

    public void Interact()
    {
        if (isActivated) return;

        isActivated = true;

        // Візуальний фідбек
        if (switchRenderer != null && activatedMaterial != null)
            switchRenderer.material = activatedMaterial;

        // Анімація повороту
        transform.localRotation = Quaternion.Euler(activatedRotation);

        // Звук
        if (switchSound != null)
            AudioSource.PlayClipAtPoint(switchSound, transform.position);

        // Відкриваємо пов'язані двері
        foreach (DoorController door in linkedDoors)
        {
            if (door != null)
                door.OpenDoor();
        }

        Debug.Log($"[Switch] {gameObject.name} activated! Opened {linkedDoors.Length} door(s).");
    }
}
