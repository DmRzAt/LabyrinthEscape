using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
	[Header("Stamina")]
	public float maxStamina = 100f;

	public float currentStamina;

	public float regenPerSecond = 25f;

	public float regenDelay = 1f;

	private float _regenTimer;

	private PlayerStats _stats;

	public float EffectiveMax => Mathf.Max(10f, maxStamina - ((_stats != null) ? _stats.StaminaPenalty : 0f));

	public static event Action<float, float> OnStaminaChanged;

	private void Start()
	{
		_stats = GetComponent<PlayerStats>();
		currentStamina = EffectiveMax;
		PlayerStamina.OnStaminaChanged?.Invoke(currentStamina, EffectiveMax);
	}

	private void Update()
	{
		float effectiveMax = EffectiveMax;
		if (currentStamina > effectiveMax)
		{
			currentStamina = effectiveMax;
		}
		if (_regenTimer > 0f)
		{
			_regenTimer -= Time.deltaTime;
		}
		else if (currentStamina < effectiveMax)
		{
			currentStamina = Mathf.Min(effectiveMax, currentStamina + regenPerSecond * Time.deltaTime);
			PlayerStamina.OnStaminaChanged?.Invoke(currentStamina, effectiveMax);
		}
	}

	public bool TryUse(float amount)
	{
		if (currentStamina < amount)
		{
			return false;
		}
		currentStamina -= amount;
		_regenTimer = regenDelay;
		PlayerStamina.OnStaminaChanged?.Invoke(currentStamina, EffectiveMax);
		return true;
	}

	public bool HasAtLeast(float amount)
	{
		return currentStamina >= amount;
	}

	public void Add(float amount)
	{
		float effectiveMax = EffectiveMax;
		currentStamina = Mathf.Clamp(currentStamina + amount, 0f, effectiveMax);
		PlayerStamina.OnStaminaChanged?.Invoke(currentStamina, effectiveMax);
	}

	public void DrainContinuous(float perSecond)
	{
		float a = currentStamina;
		currentStamina = Mathf.Max(0f, currentStamina - perSecond * Time.deltaTime);
		_regenTimer = regenDelay;
		if (!Mathf.Approximately(a, currentStamina))
		{
			PlayerStamina.OnStaminaChanged?.Invoke(currentStamina, EffectiveMax);
		}
	}
}
