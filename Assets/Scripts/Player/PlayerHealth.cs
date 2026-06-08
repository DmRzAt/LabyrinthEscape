using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
	[Header("HP")]
	public int maxHP = 100;

	public int currentHP;

	[Header("Invincibility")]
	public float invincibilityTime = 0.6f;

	[Header("Block")]
	public bool isBlocking;

	public float blockDamageMultiplier = 0.3f;

	public float blockStaminaPerHitPoint = 0.8f;

	private float _invincibilityTimer;

	private Transform _lastAttacker;

	private PlayerStamina _stamina;

	private SwordCombat _swordCombat;

	private AudioSource _audio;

	private AudioClip _clang;

	private AudioClip _blockThud;

	public bool IsDead { get; private set; }

	public bool IsInvincible => _invincibilityTimer > 0f;

	public static event Action<int, int> OnHealthChanged;

	public static event Action OnPlayerDied;

	private void Start()
	{
		currentHP = maxHP;
		_stamina = GetComponent<PlayerStamina>();
		_swordCombat = GetComponent<SwordCombat>();
		_audio = GetComponent<AudioSource>();
		if (_audio == null)
		{
			_audio = base.gameObject.AddComponent<AudioSource>();
		}
		_clang = ProceduralSfx.Clang(1);
		_blockThud = ProceduralSfx.Thud(1);
		PlayerHealth.OnHealthChanged?.Invoke(currentHP, maxHP);
	}

	private void BlockFeedback(AudioClip clip, float intensity, float shake)
	{
		Camera main = Camera.main;
		if (main != null)
		{
			CombatVFX.SpawnHit(main.transform.position + main.transform.forward * 1.1f, -main.transform.forward, intensity);
		}
		CameraShake.Shake(0.12f, shake);
		CameraShake.Punch(new Vector3(-3.5f * intensity, UnityEngine.Random.Range(-2f, 2f), 0f));
		if (_audio != null && clip != null)
		{
			_audio.pitch = UnityEngine.Random.Range(0.96f, 1.05f);
			_audio.PlayOneShot(clip);
		}
	}

	public void ReceiveAttack(int amount, EnemyAI attacker)
	{
		_lastAttacker = ((attacker != null) ? attacker.transform : null);
		TakeDamage(amount);
		_lastAttacker = null;
	}

	private void Update()
	{
		if (_invincibilityTimer > 0f)
		{
			_invincibilityTimer -= Time.deltaTime;
		}
	}

	public void TakeDamage(int amount)
	{
		if (IsDead || IsInvincible)
		{
			return;
		}
		bool flag = false;
		if (_swordCombat != null && _swordCombat.IsParryWindow)
		{
			_invincibilityTimer = invincibilityTime;
			BlockFeedback(_clang, 1.6f, 0.06f);
			return;
		}
		if (isBlocking)
		{
			int num = Mathf.Max(1, Mathf.RoundToInt((float)amount * blockDamageMultiplier));
			float amount2 = (float)(amount - num) * blockStaminaPerHitPoint;
			if (_stamina == null || _stamina.TryUse(amount2))
			{
				amount = num;
				flag = true;
				BlockFeedback(_blockThud, 0.8f, 0.05f);
			}
		}
		currentHP = Mathf.Max(0, currentHP - amount);
		_invincibilityTimer = invincibilityTime;
		PlayerHealth.OnHealthChanged?.Invoke(currentHP, maxHP);
		CameraShake.Shake(0.15f, flag ? 0.05f : 0.15f);
		CameraShake.Punch(new Vector3(flag ? 1.5f : 3.5f, HitYawKick(), 0f));
		if (currentHP <= 0)
		{
			Die();
		}
	}

	private float HitYawKick()
	{
		if (_lastAttacker == null)
		{
			return UnityEngine.Random.Range(-2f, 2f);
		}
		Camera main = Camera.main;
		Transform transform = ((main != null) ? main.transform : base.transform);
		Vector3 vector = _lastAttacker.position - base.transform.position;
		vector.y = 0f;
		if (vector.sqrMagnitude < 0.0001f)
		{
			return UnityEngine.Random.Range(-2f, 2f);
		}
		return Vector3.Dot(vector.normalized, transform.right) * 3f + UnityEngine.Random.Range(-1f, 1f);
	}

	public void Heal(int amount)
	{
		if (!IsDead)
		{
			currentHP = Mathf.Min(maxHP, currentHP + amount);
			PlayerHealth.OnHealthChanged?.Invoke(currentHP, maxHP);
		}
	}

	private void Die()
	{
		IsDead = true;
		PlayerHealth.OnPlayerDied?.Invoke();
		GameManager.Instance?.LoseGame();
	}
}
