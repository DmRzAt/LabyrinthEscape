using UnityEngine;

/// <summary>
/// Klucz do zebrania. Po interakcji (E) lub wejściu w trigger — dodaje klucz do GameManager.
/// </summary>
public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Анімація обертання та підстрибування
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);

        Vector3 newPos = startPos;
        newPos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = newPos;
    }

    /// <summary>
    /// Interakcja przez raycast (klawisz E).
    /// </summary>
    public void Interact()
    {
        CollectKey();
    }

    /// <summary>
    /// Trigger — gracz wchodzi w collider klucza.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectKey();
        }
    }

    private void CollectKey()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectKey();
        }

        // Dźwięk
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Debug.Log($"[KeyPickup] Key collected: {gameObject.name}");
        Destroy(gameObject);
    }
}
