using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
	[Header("HP")]
	public int maxHP = 50;

	public int currentHP;

	[Header("FX")]
	public GameObject deathEffect;

	public float destroyDelay = 0.1f;

	[Header("Hit flash")]
	public Color flashColor = new Color(1f, 1f, 1f, 1f);

	public float flashDuration = 0.07f;

	public float flashEmission = 2.5f;

	[Header("Health Bar")]
	[Tooltip("Floating HP bar above this enemy. Bosses use the big screen bar instead.")]
	public bool showHealthBar = true;

	[Tooltip("World height (m) of the floating bar above the pivot.")]
	public float healthBarHeight = 2.2f;

	[Header("Boss")]
	[Tooltip("Drives the large screen-space boss bar instead of a floating one.")]
	public bool isBoss;

	public string bossName = "Skeleton Lord";

	private Animator _animator;

	private EnemyAI _ai;

	private bool _dead;

	private Renderer[] _renderers;

	private MaterialPropertyBlock _mpb;

	private Coroutine _flashCo;

	private Coroutine _knockCo;

	private NavMeshAgent _agent;

	private EnemyAudio _audio;

	public bool IsDead => _dead;

	private EnemyAudio Audio
	{
		get
		{
			if (_audio == null)
			{
				_audio = GetComponent<EnemyAudio>();
				if (_audio == null)
				{
					_audio = base.gameObject.AddComponent<EnemyAudio>();
				}
			}
			return _audio;
		}
	}

	public event Action<EnemyHealth> Died;

	private void Start()
	{
		currentHP = maxHP;
		_ai = GetComponent<EnemyAI>();
		_agent = GetComponent<NavMeshAgent>();
		_renderers = GetComponentsInChildren<Renderer>();
		_mpb = new MaterialPropertyBlock();
		if (isBoss)
		{
			BossHealthBar.Register(this, bossName);
		}
		else if (showHealthBar && GetComponent<EnemyHealthBar>() == null)
		{
			base.gameObject.AddComponent<EnemyHealthBar>().Init(this, healthBarHeight);
		}
	}

	public void FlashHit(float emissionScale = 1f)
	{
		if (!_dead && _renderers != null && _renderers.Length != 0)
		{
			if (_flashCo != null)
			{
				StopCoroutine(_flashCo);
			}
			_flashCo = StartCoroutine(FlashRoutine(emissionScale));
		}
	}

	private IEnumerator FlashRoutine(float emissionScale)
	{
		Renderer[] renderers = _renderers;
		foreach (Renderer renderer in renderers)
		{
			if (!(renderer == null))
			{
				renderer.GetPropertyBlock(_mpb);
				_mpb.SetColor("_BaseColor", flashColor);
				_mpb.SetColor("_EmissionColor", flashColor * (flashEmission * emissionScale));
				renderer.SetPropertyBlock(_mpb);
			}
		}
		yield return new WaitForSecondsRealtime(flashDuration);
		renderers = _renderers;
		foreach (Renderer renderer2 in renderers)
		{
			if (renderer2 != null)
			{
				renderer2.SetPropertyBlock(null);
			}
		}
		_flashCo = null;
	}

	public void ApplyKnockback(Vector3 dir, float force)
	{
		if (!_dead && !(force <= 0f) && !(dir.sqrMagnitude < 0.0001f))
		{
			if (_knockCo != null)
			{
				StopCoroutine(_knockCo);
			}
			_knockCo = StartCoroutine(KnockbackRoutine(dir.normalized, force));
		}
	}

	private IEnumerator KnockbackRoutine(Vector3 dir, float force)
	{
		float t = 0f;
		while (t < 0.18f && !_dead)
		{
			float num = Mathf.Lerp(force, 0f, t / 0.18f);
			Vector3 vector = dir * (num * Time.deltaTime);
			if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
			{
				_agent.Move(vector);
			}
			else
			{
				base.transform.position += vector;
			}
			t += Time.deltaTime;
			yield return null;
		}
		_knockCo = null;
	}

	public void TakeDamage(int amount)
	{
		TakeDamage(amount, 0.25f);
	}

	public void TakeDamage(int amount, float staggerDuration)
	{
		if (_dead)
		{
			return;
		}
		if (_animator == null)
		{
			_animator = GetComponentInChildren<Animator>();
		}
		currentHP = Mathf.Max(0, currentHP - amount);
		float emissionScale = Mathf.Clamp(1f + (float)amount / Mathf.Max(1f, (float)maxHP * 0.5f), 1f, 2.2f);
		FlashHit(emissionScale);
		if (currentHP > 0)
		{
			Audio.PlayHurt();
		}
		if (_ai == null)
		{
			_ai = GetComponent<EnemyAI>();
		}
		if (_ai != null)
		{
			Transform playerTransform = EnemyAI.PlayerTransform;
			if (playerTransform != null)
			{
				_ai.ReactToDamage(playerTransform.position);
			}
		}
		if (currentHP <= 0)
		{
			Die();
		}
		else if (_ai != null)
		{
			_ai.Stagger(staggerDuration);
		}
		else if (HasAnimParam("Hurt"))
		{
			_animator.SetTrigger("Hurt");
		}
	}

	private bool HasAnimParam(string name)
	{
		if (_animator == null || _animator.runtimeAnimatorController == null)
		{
			return false;
		}
		AnimatorControllerParameter[] parameters = _animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == name)
			{
				return true;
			}
		}
		return false;
	}

	private void Die()
	{
		_dead = true;
		GameManager.Instance?.AddKill();
		this.Died?.Invoke(this);
		bool flag = HasAnimParam("Die");
		if (_animator != null)
		{
			if (HasAnimParam("Alert"))
			{
				_animator.ResetTrigger("Alert");
			}
			if (HasAnimParam("Hurt"))
			{
				_animator.ResetTrigger("Hurt");
			}
			if (HasAnimParam("Attack"))
			{
				_animator.ResetTrigger("Attack");
			}
			_animator.speed = 1f;
		}
		if (flag)
		{
			_animator.SetTrigger("Die");
		}
		if (_ai != null)
		{
			_ai.MarkDead();
			_ai.CancelInvoke();
			_ai.enabled = false;
		}
		NavMeshAgent component = GetComponent<NavMeshAgent>();
		if (component != null && component.isOnNavMesh)
		{
			component.isStopped = true;
		}
		if (component != null)
		{
			component.enabled = false;
		}
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		if (deathEffect != null)
		{
			UnityEngine.Object.Instantiate(deathEffect, base.transform.position, Quaternion.identity);
		}
		Audio.PlayDeath();
		StartCoroutine(DespawnRoutine(flag));
	}

	private IEnumerator DespawnRoutine(bool hadDeathAnim)
	{
		float seconds = (hadDeathAnim ? Mathf.Max(destroyDelay, 2.5f) : destroyDelay);
		yield return new WaitForSeconds(seconds);
		float t = 0f;
		float sink = 1.2f;
		Vector3 start = base.transform.position;
		while (t < sink)
		{
			t += Time.deltaTime;
			base.transform.position = start + Vector3.down * (t / sink * 1.6f);
			yield return null;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
