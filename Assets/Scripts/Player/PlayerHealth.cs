using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int currentHP;

    public static event System.Action<int, int> OnHealthChanged;

    void Start()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    [Header("Block")]
    public bool isBlocking = false;
    public float blockDamageMultiplier = 0.3f; // 70% reduction при блоці

    public void TakeDamage(int amount)
    {
        if (isBlocking) amount = Mathf.Max(1, Mathf.RoundToInt(amount * blockDamageMultiplier));
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        CameraShake.Shake(0.15f, isBlocking ? 0.05f : 0.15f);
        if (currentHP <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        GameManager.Instance?.LoseGame();
    }
}
