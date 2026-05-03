using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenPerSecond = 25f;
    public float regenDelay = 1.0f;     // затримка перед регенерацією після витрати

    public static event System.Action<float, float> OnStaminaChanged;

    private float _regenTimer;

    void Start()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    void Update()
    {
        if (_regenTimer > 0f) _regenTimer -= Time.deltaTime;
        else if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool TryUse(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        _regenTimer = regenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public bool HasAtLeast(float amount) => currentStamina >= amount;

    public void DrainContinuous(float perSecond)
    {
        currentStamina = Mathf.Max(0f, currentStamina - perSecond * Time.deltaTime);
        _regenTimer = regenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
