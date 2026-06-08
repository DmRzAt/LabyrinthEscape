using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Overlay")]
    public Image overlay;
    public Color hurtColor = new Color(0.65f, 0f, 0f, 1f);

    [Header("Hit flash")]
    public float flashAlpha = 0.55f;
    public float flashDecay = 2.2f;

    [Header("Low HP pulse")]
    [Range(0f, 1f)] public float lowHpThreshold = 0.35f;
    public float pulseSpeed = 3.5f;
    public float pulseMaxAlpha = 0.42f;

    float _flash;
    int _lastHp = -1;
    PlayerHealth _health;

    void Awake() => _health = GetComponent<PlayerHealth>();
    void OnEnable() => PlayerHealth.OnHealthChanged += OnHealthChanged;
    void OnDisable() => PlayerHealth.OnHealthChanged -= OnHealthChanged;

    void OnHealthChanged(int cur, int max)
    {
        if (_lastHp >= 0 && cur < _lastHp) _flash = Mathf.Max(_flash, flashAlpha);
        _lastHp = cur;
    }

    void Update()
    {
        if (overlay == null) return;

        _flash = Mathf.MoveTowards(_flash, 0f, flashDecay * Time.deltaTime);

        float pulse = 0f;
        if (_health != null && !_health.IsDead && _health.maxHP > 0)
        {
            float ratio = (float)_health.currentHP / _health.maxHP;
            if (ratio < lowHpThreshold)
            {
                float severity = 1f - ratio / lowHpThreshold;
                pulse = (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed)) * pulseMaxAlpha * severity;
            }
        }

        var c = hurtColor;
        c.a = Mathf.Clamp01(Mathf.Max(_flash, pulse));
        overlay.color = c;
    }
}
