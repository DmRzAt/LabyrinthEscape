using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 50;
    public int currentHP;

    [Header("FX")]
    public GameObject deathEffect;
    public float destroyDelay = 0.1f;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(gameObject, destroyDelay);
    }
}
