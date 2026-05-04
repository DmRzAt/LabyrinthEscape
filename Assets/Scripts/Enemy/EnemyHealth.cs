using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event System.Action<EnemyHealth> Died;

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

    private Animator _animator;
    private bool _dead;

    public void TakeDamage(int amount)
    {
        if (_dead) return;
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP <= 0) Die();
        else if (_animator != null) _animator.SetTrigger("Hurt");
    }

    void Die()
    {
        _dead = true;
        Died?.Invoke(this);
        if (_animator != null) _animator.SetTrigger("Die");
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(gameObject, destroyDelay);
    }
}
