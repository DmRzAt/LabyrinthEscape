using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusEffects : MonoBehaviour
{
    public class Effect
    {
        public string id;
        public string label;
        public Color color = Color.white;
        public float speedMultiplier;
        public float jumpMultiplier;
        public float healPerSecond;
        public float staminaPerSecond;
        public float timeRemaining;
        public float duration;
    }

    private readonly List<Effect> _effects = new List<Effect>();
    private PlayerHealth _health;
    private PlayerStamina _stamina;
    private float _healCarry;

    void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _stamina = GetComponent<PlayerStamina>();
    }

    public float SpeedMultiplier { get; private set; } = 1f;
    public float JumpMultiplier { get; private set; } = 1f;

    public IReadOnlyList<Effect> Active => _effects;

    public static event System.Action OnEffectsChanged;

    public void Apply(string id, float speedMultiplier, float duration, float jumpMultiplier = 1f, float healPerSecond = 0f, string label = null, Color? color = null, float staminaPerSecond = 0f)
    {
        var e = _effects.Find(x => x.id == id);
        if (e != null)
        {
            e.label = label ?? id;
            e.color = color ?? Color.white;
            e.speedMultiplier = speedMultiplier;
            e.jumpMultiplier = jumpMultiplier;
            e.healPerSecond = healPerSecond;
            e.staminaPerSecond = staminaPerSecond;
            e.timeRemaining = duration;
            e.duration = duration;
        }
        else
        {
            _effects.Add(new Effect
            {
                id = id,
                label = label ?? id,
                color = color ?? Color.white,
                speedMultiplier = speedMultiplier,
                jumpMultiplier = jumpMultiplier,
                healPerSecond = healPerSecond,
                staminaPerSecond = staminaPerSecond,
                timeRemaining = duration,
                duration = duration
            });
        }
        Recalculate();
    }

    public void Remove(string id)
    {
        if (_effects.RemoveAll(x => x.id == id) > 0) Recalculate();
    }

    public void ClearAll()
    {
        if (_effects.Count == 0) return;
        _effects.Clear();
        Recalculate();
    }

    void Update()
    {
        if (_effects.Count == 0) return;
        bool changed = false;
        float healRate = 0f;
        float staminaRate = 0f;
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            _effects[i].timeRemaining -= Time.deltaTime;
            healRate += _effects[i].healPerSecond;
            staminaRate += _effects[i].staminaPerSecond;
            if (_effects[i].timeRemaining <= 0f) { _effects.RemoveAt(i); changed = true; }
        }
        if (healRate > 0f && _health != null)
        {
            _healCarry += healRate * Time.deltaTime;
            int whole = Mathf.FloorToInt(_healCarry);
            if (whole > 0) { _health.Heal(whole); _healCarry -= whole; }
        }
        if (staminaRate > 0f && _stamina != null)
            _stamina.Add(staminaRate * Time.deltaTime);
        if (changed) Recalculate();
    }

    void Recalculate()
    {
        float s = 1f, j = 1f;
        for (int i = 0; i < _effects.Count; i++)
        {
            s *= _effects[i].speedMultiplier;
            j *= _effects[i].jumpMultiplier;
        }
        SpeedMultiplier = s;
        JumpMultiplier = j;
        OnEffectsChanged?.Invoke();
    }
}
