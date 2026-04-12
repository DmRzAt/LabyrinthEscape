using UnityEngine;

/// <summary>
/// System HP gracza. Zarządza zdrowiem, obrażeniami i śmiercią.
/// Współpracuje z GameHUD (aktualizacja UI) i GameManager (śmierć gracza).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Damage")]
    [SerializeField] private float damageCooldown = 0.5f; // невразливість після удару

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    private float lastDamageTime = -999f;
    private bool isDead = false;

    // Event для HUD
    public event System.Action<float> OnHealthChanged;

    private GameHUD hud;

    private void Start()
    {
        currentHealth = maxHealth;
        hud = FindFirstObjectByType<GameHUD>();
        UpdateUI();
    }

    /// <summary>
    /// Otrzymaj obrażenia.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        Debug.Log($"[PlayerHealth] Took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (hurtSound != null)
            AudioSource.PlayClipAtPoint(hurtSound, transform.position);

        UpdateUI();
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Odzyskaj zdrowie (np. apteczka).
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        UpdateUI();
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);

        Debug.Log("[PlayerHealth] Player died!");

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }

    private void UpdateUI()
    {
        if (hud != null)
            hud.UpdateHP(currentHealth);
    }

    // ─────────────── Getters ───────────────
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}
