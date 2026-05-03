using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Sword")]
    public bool hasSword = false;
    public Transform sword;
    public bool autoSetupSword = true;
    public float targetSwordLength = 0.6f;

    [Header("Idle pose (де меч лежить у руці)")]
    public Vector3 idleLocalPos = new Vector3(0.35f, -0.28f, 0.6f);
    public Vector3 idleLocalEuler = new Vector3(-45f, 90f, 30f);

    [Header("Swing (Minecraft-style)")]
    public Vector3 swingRotationOffset = new Vector3(60f, -30f, 0f); // на скільки повернути меч при ударі (X = вниз, Y = вбік)
    public Vector3 swingPositionOffset = new Vector3(-0.05f, -0.05f, 0.1f); // легке зміщення при ударі
    public float swingOutTime = 0.06f;
    public float swingHoldTime = 0.03f;
    public float swingBackTime = 0.12f;
    public float cooldown = 0.25f;

    [Header("Heavy attack")]
    public float heavyHoldThreshold = 0.35f;     // скільки тримати ЛКМ для важкого удару
    public float heavyDamageMultiplier = 2.2f;
    public Vector3 heavyRotationOffset = new Vector3(90f, -45f, 0f);
    public Vector3 heavyPositionOffset = new Vector3(-0.25f, -0.1f, 0.6f);
    public float heavySwingOutTime = 0.18f;
    public float heavySwingBackTime = 0.25f;
    public float heavyCooldown = 0.6f;

    [Header("Block")]
    public Vector3 blockLocalPos = new Vector3(0.1f, -0.15f, 0.55f);
    public Vector3 blockLocalEuler = new Vector3(-30f, 60f, -10f);
    public float blockMoveTime = 0.15f;
    public float blockStaminaPerSecond = 8f;

    [Header("Stamina cost")]
    public float lightAttackStamina = 12f;
    public float heavyAttackStamina = 28f;

    [Header("Camera shake")]
    public float lightShake = 0.05f;
    public float heavyShake = 0.15f;

    [Header("Hit detection")]
    public int damage = 25;
    public Vector3 hitboxOffset = new Vector3(0f, 0.3f, 0f);
    public Vector3 hitboxSize = new Vector3(0.15f, 0.6f, 0.15f);
    public LayerMask enemyMask = ~0;

    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip hitSound;

    public Transform attackOrigin;

    private AudioSource _audio;
    private BoxCollider _hitbox;
    private float _cooldownTimer;
    private bool _isSwinging;
    private bool _isBlocking;
    private float _lmbHoldTime;
    private bool _heavyQueued;
    private PlayerStamina _stamina;
    private PlayerHealth _health;
    private System.Collections.Generic.HashSet<EnemyHealth> _hitThisSwing = new System.Collections.Generic.HashSet<EnemyHealth>();

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _stamina = GetComponent<PlayerStamina>();
        _health = GetComponent<PlayerHealth>();

        if (attackOrigin == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) attackOrigin = cam.transform;
        }

        if (sword != null) Setup();
        if (sword != null) sword.gameObject.SetActive(hasSword);
    }

    [ContextMenu("Setup In Editor")]
    public void SetupInEditor()
    {
        if (attackOrigin == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) attackOrigin = cam.transform;
        }
        if (sword == null) { Debug.LogWarning("Assign Sword first!"); return; }
        Setup();
        if (sword != null) sword.gameObject.SetActive(true);
    }

    void Setup()
    {
        Transform parent = attackOrigin != null ? attackOrigin : transform;
        if (sword.parent != parent) sword.SetParent(parent, false);

        if (autoSetupSword)
        {
            sword.localScale = Vector3.one;
            var rs = sword.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (maxDim > 0.0001f) sword.localScale = Vector3.one * (targetSwordLength / maxDim);
            }
        }

        sword.localPosition = idleLocalPos;
        sword.localEulerAngles = idleLocalEuler;

        // хітбокс на лезі
        var hitGo = sword.Find("Hitbox");
        if (hitGo == null)
        {
            var go = new GameObject("Hitbox");
            go.transform.SetParent(sword, false);
            hitGo = go.transform;
        }
        hitGo.localPosition = hitboxOffset;
        hitGo.localRotation = Quaternion.identity;

        _hitbox = hitGo.GetComponent<BoxCollider>();
        if (_hitbox == null) _hitbox = hitGo.gameObject.AddComponent<BoxCollider>();
        _hitbox.isTrigger = true;
        _hitbox.size = hitboxSize;
        _hitbox.enabled = false;

        var notifier = hitGo.GetComponent<SwordHitbox>();
        if (notifier == null) notifier = hitGo.gameObject.AddComponent<SwordHitbox>();
        notifier.owner = this;
    }

    public void EquipSword()
    {
        hasSword = true;
        if (sword != null) sword.gameObject.SetActive(true);
        Debug.Log("[PlayerAttack] Sword equipped.");
    }

    void Update()
    {
        // тримаємо idle позу коли не б'ємо і не блокуємо
        if (!_isSwinging && !_isBlocking && sword != null)
        {
            sword.localPosition = idleLocalPos;
            sword.localEulerAngles = idleLocalEuler;
            if (_hitbox != null)
            {
                _hitbox.transform.localPosition = hitboxOffset;
                _hitbox.size = hitboxSize;
            }
        }

        _cooldownTimer -= Time.deltaTime;

        if (!hasSword) return;

        // BLOCK (ПКМ)
        bool wantBlock = Input.GetMouseButton(1) && !_isSwinging;
        if (wantBlock && _stamina != null && _stamina.HasAtLeast(1f))
        {
            if (!_isBlocking) StartCoroutine(EnterBlock());
            _stamina.DrainContinuous(blockStaminaPerSecond);
        }
        else if (_isBlocking)
        {
            StartCoroutine(ExitBlock());
        }

        if (_isBlocking) return; // не атакуємо коли блокуємо

        // ATTACK input — heavy/light
        if (Input.GetMouseButton(0) && _cooldownTimer <= 0f && !_isSwinging)
            _lmbHoldTime += Time.deltaTime;

        if (Input.GetMouseButtonUp(0) && _cooldownTimer <= 0f && !_isSwinging)
        {
            bool heavy = _lmbHoldTime >= heavyHoldThreshold;
            _lmbHoldTime = 0f;
            float cost = heavy ? heavyAttackStamina : lightAttackStamina;
            if (_stamina == null || _stamina.TryUse(cost))
                StartCoroutine(SwingRoutine(heavy));
        }
    }

    IEnumerator EnterBlock()
    {
        _isBlocking = true;
        if (_health != null) _health.isBlocking = true;
        Vector3 sp = sword.localPosition;
        Quaternion sr = sword.localRotation;
        Quaternion br = Quaternion.Euler(blockLocalEuler);
        float t = 0f;
        while (t < blockMoveTime && _isBlocking)
        {
            t += Time.deltaTime;
            float k = t / blockMoveTime;
            sword.localPosition = Vector3.Lerp(sp, blockLocalPos, k);
            sword.localRotation = Quaternion.Slerp(sr, br, k);
            yield return null;
        }
    }

    IEnumerator ExitBlock()
    {
        _isBlocking = false;
        if (_health != null) _health.isBlocking = false;
        Vector3 sp = sword.localPosition;
        Quaternion sr = sword.localRotation;
        Quaternion ir = Quaternion.Euler(idleLocalEuler);
        float t = 0f;
        while (t < blockMoveTime)
        {
            t += Time.deltaTime;
            float k = t / blockMoveTime;
            sword.localPosition = Vector3.Lerp(sp, idleLocalPos, k);
            sword.localRotation = Quaternion.Slerp(sr, ir, k);
            yield return null;
        }
    }

    IEnumerator SwingRoutine(bool heavy)
    {
        _isSwinging = true;
        _cooldownTimer = heavy ? heavyCooldown : cooldown;
        _hitThisSwing.Clear();
        if (swingSound != null) _audio.PlayOneShot(swingSound);

        Vector3 startPos = idleLocalPos;
        Vector3 endPos = idleLocalPos + (heavy ? heavyPositionOffset : swingPositionOffset);
        Quaternion startRot = Quaternion.Euler(idleLocalEuler);
        Quaternion endRot = Quaternion.Euler(idleLocalEuler + (heavy ? heavyRotationOffset : swingRotationOffset));
        float outTime = heavy ? heavySwingOutTime : swingOutTime;
        float backTime = heavy ? heavySwingBackTime : swingBackTime;
        _heavyQueued = heavy;

        if (_hitbox != null) _hitbox.enabled = true;
        float t = 0f;
        while (t < outTime)
        {
            t += Time.deltaTime;
            float k = t / outTime;
            sword.localPosition = Vector3.Lerp(startPos, endPos, k);
            sword.localRotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        sword.localPosition = endPos;
        sword.localRotation = endRot;

        yield return new WaitForSeconds(swingHoldTime);
        if (_hitbox != null) _hitbox.enabled = false;

        t = 0f;
        while (t < backTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / backTime);
            sword.localPosition = Vector3.Lerp(endPos, startPos, k);
            sword.localRotation = Quaternion.Slerp(endRot, startRot, k);
            yield return null;
        }
        sword.localPosition = startPos;
        sword.localRotation = startRot;

        _isSwinging = false;
    }

    public void OnHitboxTrigger(Collider other)
    {
        if (!_isSwinging || _hitbox == null || !_hitbox.enabled) return;
        if (other.transform.IsChildOf(transform) || other.transform == transform) return;
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;

        var hp = other.GetComponentInParent<EnemyHealth>();
        if (hp != null && !_hitThisSwing.Contains(hp))
        {
            _hitThisSwing.Add(hp);
            int dmg = _heavyQueued ? Mathf.RoundToInt(damage * heavyDamageMultiplier) : damage;
            hp.TakeDamage(dmg);
            CameraShake.Shake(0.12f, _heavyQueued ? heavyShake : lightShake);
            if (hitSound != null) _audio.PlayOneShot(hitSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_hitbox == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = _hitbox.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_hitbox.center, _hitbox.size);
    }
}
