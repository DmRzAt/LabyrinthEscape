using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator handsAnimator;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Transform swordObject;
    [SerializeField] private TrailRenderer bladeTrail;

    [Header("Animator states")]
    [SerializeField] private string armedIdleState = "Idle";
    [SerializeField] private string unarmedIdleState = "FistsIdle";
    [SerializeField] private string blockState = "Block";

    [Header("Light attack")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float windup = 0.16f;
    [SerializeField] private float damageWindow = 0.22f;
    [SerializeField] private float recovery = 0.22f;
    [SerializeField] private float shake = 0.08f;
    [SerializeField] private float hitStop = 0.05f;
    [SerializeField] private float stagger = 0.35f;

    [Header("Heavy attack (hold LMB)")]
    [SerializeField] private float heavyHoldThreshold = 0.3f;
    [SerializeField] private float heavyDamageMultiplier = 2.2f;
    [SerializeField] private float heavyWindup = 0.34f;
    [SerializeField] private float heavyWindow = 0.26f;
    [SerializeField] private float heavyRecovery = 0.38f;
    [SerializeField] private float heavySpeed = 0.7f;
    [SerializeField] private float heavyShake = 0.16f;
    [SerializeField] private float heavyHitStop = 0.08f;
    [SerializeField] private float heavyStagger = 0.6f;

    [Header("Stamina")]
    [SerializeField, Range(0f, 100f)] private float lightAttackStamina = 12f;
    [SerializeField, Range(0f, 100f)] private float heavyAttackStamina = 28f;
    [SerializeField, Range(0f, 50f)] private float blockStaminaPerSecond = 8f;
    [SerializeField, Range(0f, 20f)] private float minStaminaToBlock = 1f;

    [Header("Block / parry")]
    [SerializeField, Range(0f, 0.5f)] private float parryWindow = 0.22f;

    [Header("Impact")]
    [SerializeField] private float knockback = 2.5f;
    [SerializeField] private float heavyKnockback = 5.5f;

    [Header("Hit detection")]
    [SerializeField] private float range = 2.2f;
    [SerializeField] private float radius = 0.6f;
    [SerializeField] private float comboResetTime = 1.2f;
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private LayerMask losBlockMask = ~0;
    [SerializeField, Range(0f, 30f)] private float swingNoiseRadius = 8f;

    [Header("Audio")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip heavySwingSound;
    [SerializeField] private AudioClip hitSound;

    private readonly string[] _lightStates = { "Slash1", "Slash2" };
    private const string HeavyState = "Slash2";

    private AudioSource _audio;
    private PlayerHealth _health;
    private PlayerStamina _stamina;
    private bool _equipped;
    private bool _attacking;
    private bool _blocking;
    private int _combo;
    private float _lastAttack;
    private float _lmbHold;
    private float _blockStartTime;
    private bool _bufferedAttack;
    private bool _bufferedHeavy;
    private readonly Collider[] _overlap = new Collider[24];

    public bool IsBlocking => _blocking;
    public bool IsAttacking => _attacking;
    public bool IsParryWindow => _blocking && (Time.time - _blockStartTime) <= parryWindow;

    private int _curDamage;
    private float _curShake, _curHitStop, _curStagger, _curKnockback;
    private bool _curHeavy;

    private int _weaponDamage;
    private float _weaponSpeed = 1f;

    void Start()
    {
        if (handsAnimator == null)
        {
            var arms = GameObject.Find("FPS_Hands");
            if (arms != null) handsAnimator = arms.GetComponent<Animator>();
        }
        if (attackOrigin == null && Camera.main != null) attackOrigin = Camera.main.transform;
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _health = GetComponent<PlayerHealth>();
        _stamina = GetComponent<PlayerStamina>();
        _weaponDamage = damage;
        SetEquipped(false);
    }

    public void SetWeapon(string weaponName)
    {
        var s = WeaponStats.Get(weaponName);
        _weaponDamage = s.damage;
        _weaponSpeed = s.speed <= 0f ? 1f : s.speed;
    }

    public void SetEquipped(bool on)
    {
        _equipped = on;
        if (swordObject != null) swordObject.gameObject.SetActive(on);
        if (!on) StopBlocking();
        if (handsAnimator != null)
            handsAnimator.CrossFadeInFixedTime(on ? armedIdleState : unarmedIdleState, 0.15f, 0);
    }

    void Update()
    {
        bool canAct = _equipped
                      && Cursor.lockState == CursorLockMode.Locked
                      && (_health == null || !_health.IsDead);
        var mouse = Mouse.current;
        if (!canAct || mouse == null)
        {
            StopBlocking();
            _lmbHold = 0f;
            return;
        }

        bool hasBlockStamina = _stamina == null || _stamina.HasAtLeast(minStaminaToBlock);
        bool wantBlock = mouse.rightButton.isPressed && !_attacking && hasBlockStamina;
        if (wantBlock)
        {
            if (!_blocking) StartBlocking();
            if (_stamina != null) _stamina.DrainContinuous(blockStaminaPerSecond);
            _lmbHold = 0f;
            return;
        }
        if (_blocking) StopBlocking();

        if (_attacking)
        {
            if (mouse.leftButton.isPressed) _lmbHold += Time.deltaTime;
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _bufferedAttack = true;
                _bufferedHeavy = _lmbHold >= heavyHoldThreshold;
                _lmbHold = 0f;
            }
            return;
        }

        if (mouse.leftButton.isPressed) _lmbHold += Time.deltaTime;
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            bool heavy = _lmbHold >= heavyHoldThreshold;
            _lmbHold = 0f;
            if (TrySpendAndAttack(heavy)) return;
        }
    }

    bool TrySpendAndAttack(bool heavy)
    {
        float staminaCost = heavy ? heavyAttackStamina : lightAttackStamina;
        if (_stamina != null && !_stamina.TryUse(staminaCost)) return false;
        StartCoroutine(Attack(heavy));
        return true;
    }

    void StartBlocking()
    {
        _blocking = true;
        _blockStartTime = Time.time;
        if (_health != null) _health.isBlocking = true;
        if (handsAnimator != null) handsAnimator.CrossFadeInFixedTime(blockState, 0.08f, 0);
    }

    void StopBlocking()
    {
        if (!_blocking) return;
        _blocking = false;
        if (_health != null) _health.isBlocking = false;
        if (_equipped && handsAnimator != null) handsAnimator.CrossFadeInFixedTime(armedIdleState, 0.12f, 0);
    }

    IEnumerator Attack(bool heavy)
    {
        _attacking = true;

        string state;
        float wind, window, rec;
        float spd = _weaponSpeed <= 0f ? 1f : _weaponSpeed;
        if (heavy)
        {
            state = HeavyState;
            wind = heavyWindup / spd; window = heavyWindow; rec = heavyRecovery / spd;
            _curDamage = Mathf.RoundToInt(_weaponDamage * heavyDamageMultiplier);
            _curShake = heavyShake; _curHitStop = heavyHitStop; _curStagger = heavyStagger;
            _curKnockback = heavyKnockback; _curHeavy = true;
            if (handsAnimator != null) handsAnimator.speed = heavySpeed * spd;
        }
        else
        {
            if (Time.time - _lastAttack > comboResetTime) _combo = 0;
            state = _lightStates[_combo % _lightStates.Length];
            _combo++;
            wind = windup / spd; window = damageWindow; rec = recovery / spd;
            _curDamage = _weaponDamage;
            _curShake = shake; _curHitStop = hitStop; _curStagger = stagger;
            _curKnockback = knockback; _curHeavy = false;
            if (handsAnimator != null) handsAnimator.speed = spd;
        }
        _lastAttack = Time.time;

        if (handsAnimator != null) handsAnimator.CrossFadeInFixedTime(state, 0.05f, 0);
        var clip = heavy ? (heavySwingSound != null ? heavySwingSound : swingSound) : swingSound;
        PlayVaried(clip, heavy ? 0.85f : 1f);

        EnemyAI.NotifyNoise(transform.position, swingNoiseRadius);

        UpdateTrailTip();
        if (bladeTrail != null) { bladeTrail.Clear(); bladeTrail.emitting = true; }

        yield return new WaitForSeconds(wind);

        var hitThisSwing = new HashSet<EnemyHealth>();
        float t = 0f;
        while (t < window)
        {
            if (_health != null && _health.IsDead) { EndAttack(); yield break; }
            HitCheck(hitThisSwing);
            t += Time.deltaTime;
            yield return null;
        }

        if (bladeTrail != null) bladeTrail.emitting = false;

        yield return new WaitForSeconds(rec);
        EndAttack();

        if (_bufferedAttack)
        {
            _bufferedAttack = false;
            TrySpendAndAttack(_bufferedHeavy);
        }
    }

    void EndAttack()
    {
        if (bladeTrail != null) bladeTrail.emitting = false;
        if (handsAnimator != null) handsAnimator.speed = 1f;
        _attacking = false;
    }

    void UpdateTrailTip()
    {
        if (bladeTrail == null) return;
        Transform socket = bladeTrail.transform.parent;
        if (socket == null) return;

        Renderer weapon = null;
        foreach (Transform c in socket)
        {
            if (c == bladeTrail.transform || !c.gameObject.activeInHierarchy) continue;
            var r = c.GetComponentInChildren<Renderer>();
            if (r != null) { weapon = r; break; }
        }
        if (weapon == null) return;

        var mf = weapon.GetComponent<MeshFilter>();
        Bounds mb = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : weapon.localBounds;
        Vector3 ext = mb.extents;
        int ax = (ext.x >= ext.y && ext.x >= ext.z) ? 0 : (ext.y >= ext.z ? 1 : 2);
        Vector3 tipLocal = mb.center; tipLocal[ax] += ext[ax];
        bladeTrail.transform.position = weapon.transform.TransformPoint(tipLocal);
    }

    void HitCheck(HashSet<EnemyHealth> already)
    {
        if (attackOrigin == null) return;
        Vector3 origin = attackOrigin.position;
        Vector3 fwd = attackOrigin.forward;
        Vector3 center = origin + fwd * (range * 0.5f);

        int n = Physics.OverlapSphereNonAlloc(center, radius + range * 0.5f, _overlap, enemyMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var col = _overlap[i];
            if (col == null) continue;
            var hp = col.GetComponentInParent<EnemyHealth>();
            if (hp == null || already.Contains(hp)) continue;

            Vector3 to = hp.transform.position + Vector3.up - origin;
            float dist = to.magnitude;
            if (dist > range + 0.6f) continue;
            if (Vector3.Dot(fwd, to.normalized) < 0.25f) continue;

            if (Physics.Raycast(origin + fwd * 0.25f, to.normalized, out var los, dist, losBlockMask, QueryTriggerInteraction.Ignore))
            {
                if (los.collider.GetComponentInParent<EnemyHealth>() != hp) continue;
            }

            already.Add(hp);
            hp.TakeDamage(_curDamage, _curStagger);

            Vector3 kbDir = hp.transform.position - origin; kbDir.y = 0f;
            hp.ApplyKnockback(kbDir.normalized, _curKnockback);

            bool killed = hp.currentHP <= 0;
            Vector3 impact = col.ClosestPoint(origin);
            CombatVFX.SpawnHit(impact, (origin - impact).normalized, _curHeavy ? 1.8f : 1f);
            CameraShake.Shake(0.1f, _curShake);
            CameraShake.Punch(new Vector3(-(_curShake > 0.1f ? 4f : 2.5f), Random.Range(-1.5f, 1.5f), 0f));
            CombatFeedback.HitStop(_curHitStop);
            PlayVaried(hitSound, _curHeavy ? 0.8f : 1f);

            Hitmarker.Flash(killed);
            if (_curHeavy || killed) PostFXPunch.Punch(_curHeavy ? 1f : 0.7f);
        }
    }

    void PlayVaried(AudioClip clip, float basePitch)
    {
        if (clip == null || _audio == null) return;
        _audio.pitch = basePitch * Random.Range(0.95f, 1.05f);
        _audio.PlayOneShot(clip);
    }
}
