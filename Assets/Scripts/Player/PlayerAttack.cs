using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Sword")]
    [SerializeField] private bool hasSword = false;
    [SerializeField] private Transform sword;
    [SerializeField] private bool autoSetupSword = true;
    [SerializeField, Range(0.1f, 2f)] private float targetSwordLength = 0.6f;

    [Header("Idle pose")]
    [SerializeField] private Vector3 idleLocalPos = new Vector3(0.35f, -0.28f, 0.6f);
    [SerializeField] private Vector3 idleLocalEuler = new Vector3(-45f, 90f, 30f);

    [Header("Swing (Minecraft-style)")]
    [SerializeField] private Vector3 swingRotationOffset = new Vector3(60f, -30f, 0f);
    [SerializeField] private Vector3 swingPositionOffset = new Vector3(-0.05f, -0.05f, 0.1f);
    [SerializeField, Range(0.01f, 1f)] private float swingOutTime = 0.06f;
    [SerializeField, Range(0f, 1f)] private float swingHoldTime = 0.03f;
    [SerializeField, Range(0.01f, 1f)] private float swingBackTime = 0.12f;
    [SerializeField, Range(0.05f, 2f)] private float cooldown = 0.25f;

    [Header("Heavy attack")]
    [SerializeField, Range(0.1f, 2f)] private float heavyHoldThreshold = 0.35f;
    [SerializeField, Range(1f, 5f)] private float heavyDamageMultiplier = 2.2f;
    [SerializeField] private Vector3 heavyRotationOffset = new Vector3(90f, -45f, 0f);
    [SerializeField] private Vector3 heavyPositionOffset = new Vector3(-0.25f, -0.1f, 0.6f);
    [SerializeField, Range(0.01f, 1.5f)] private float heavySwingOutTime = 0.18f;
    [SerializeField, Range(0.01f, 1.5f)] private float heavySwingBackTime = 0.25f;
    [SerializeField, Range(0.05f, 3f)] private float heavyCooldown = 0.6f;

    [Header("Block")]
    [SerializeField] private Vector3 blockLocalPos = new Vector3(0.1f, -0.15f, 0.55f);
    [SerializeField] private Vector3 blockLocalEuler = new Vector3(-30f, 60f, -10f);
    [SerializeField, Range(0.01f, 1f)] private float blockMoveTime = 0.15f;
    [SerializeField, Range(0f, 50f)] private float blockStaminaPerSecond = 8f;

    [Header("Stamina cost")]
    [SerializeField, Range(0f, 100f)] private float lightAttackStamina = 12f;
    [SerializeField, Range(0f, 100f)] private float heavyAttackStamina = 28f;

    [Header("Camera shake")]
    [SerializeField, Range(0f, 1f)] private float lightShake = 0.05f;
    [SerializeField, Range(0f, 1f)] private float heavyShake = 0.15f;

    [Header("Hit detection")]
    [SerializeField, Range(1, 200)] private int damage = 25;
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private Vector3 hitboxSize = new Vector3(0.15f, 0.6f, 0.15f);
    [SerializeField, Range(0.5f, 4f)] private float attackRange = 2f;
    [SerializeField, Range(0.1f, 1.5f)] private float attackRadius = 0.6f;
    [SerializeField] private LayerMask enemyMask = ~0;

    [Header("Audio")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip hitSound;

    [SerializeField] private Transform attackOrigin;

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
    private readonly Collider[] _hitResults = new Collider[16];
    private static readonly Vector3 DefaultHitboxOffset = new Vector3(0f, 0.3f, 0f);

    void Start()
    {
        ValidateHitboxSettings();
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

    void OnValidate()
    {
        ValidateHitboxSettings();
    }

    [ContextMenu("Setup In Editor")]
    public void SetupInEditor()
    {
        ValidateHitboxSettings();
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
        SetSwordEquipped(true);
        Debug.Log("[PlayerAttack] Sword equipped.");
    }

    public void SetSwordEquipped(bool isEquipped)
    {
        hasSword = isEquipped;
        if (sword != null) sword.gameObject.SetActive(isEquipped);
    }

    void Update()
    {
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

        if (_isBlocking) return;

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
            CheckAttackOverlaps();
            yield return null;
        }
        sword.localPosition = endPos;
        sword.localRotation = endRot;
        CheckAttackOverlaps();

        yield return new WaitForSeconds(swingHoldTime);
        CheckAttackOverlaps();
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
        TryDamageCollider(other);
    }

    private void CheckHitboxOverlaps()
    {
        if (!_isSwinging || _hitbox == null || !_hitbox.enabled) return;

        Vector3 center = _hitbox.transform.TransformPoint(_hitbox.center);
        Vector3 halfExtents = Vector3.Scale(_hitbox.size, _hitbox.transform.lossyScale) * 0.5f;
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _hitResults, _hitbox.transform.rotation, enemyMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            TryDamageCollider(_hitResults[i]);
            _hitResults[i] = null;
        }
    }

    private void CheckAttackOverlaps()
    {
        CheckMeleeOverlaps();
        CheckHitboxOverlaps();
    }

    private void CheckMeleeOverlaps()
    {
        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 start = origin.position + origin.forward * 0.35f;
        Vector3 end = origin.position + origin.forward * attackRange;
        int count = Physics.OverlapCapsuleNonAlloc(start, end, attackRadius, _hitResults, enemyMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            TryDamageCollider(_hitResults[i]);
            _hitResults[i] = null;
        }
    }

    private void ValidateHitboxSettings()
    {
        if (hitboxOffset.sqrMagnitude > 9f)
        {
            hitboxOffset = DefaultHitboxOffset;
        }

        hitboxSize.x = Mathf.Max(0.05f, hitboxSize.x);
        hitboxSize.y = Mathf.Max(0.05f, hitboxSize.y);
        hitboxSize.z = Mathf.Max(0.05f, hitboxSize.z);
    }

    private void TryDamageCollider(Collider other)
    {
        if (other == null) return;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return;
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;

        EnemyHealth hp = other.GetComponentInParent<EnemyHealth>();
        if (hp == null || _hitThisSwing.Contains(hp)) return;

        _hitThisSwing.Add(hp);
        int dmg = _heavyQueued ? Mathf.RoundToInt(damage * heavyDamageMultiplier) : damage;
        hp.TakeDamage(dmg);
        CameraShake.Shake(0.12f, _heavyQueued ? heavyShake : lightShake);
        if (hitSound != null) _audio.PlayOneShot(hitSound);
    }

    void OnDrawGizmosSelected()
    {
        if (_hitbox != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = _hitbox.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_hitbox.center, _hitbox.size);
        }

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector3 start = origin.position + origin.forward * 0.35f;
        Vector3 end = origin.position + origin.forward * attackRange;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(start, attackRadius);
        Gizmos.DrawWireSphere(end, attackRadius);
        Gizmos.DrawLine(start + origin.up * attackRadius, end + origin.up * attackRadius);
        Gizmos.DrawLine(start - origin.up * attackRadius, end - origin.up * attackRadius);
    }
}
