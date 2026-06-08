using System.Collections;
using UnityEngine;

public class Brazier : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private int _index;

    [Header("Flame")]
    [SerializeField] private Light _flameLight;
    [SerializeField] private GameObject _flameVisual;
    [SerializeField] private Color _flameColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float _litIntensity = 3f;
    [SerializeField] private float _flameHeight = 1.2f;
    [SerializeField] private float _flameRange = 6f;

    [Header("Glow (emissive orb/crystal)")]
    [Tooltip("Renderer whose emission tracks lit state — faintly coloured when idle, blazing on flash.")]
    [SerializeField] private Renderer _glowRenderer;
    [SerializeField] private float _idleEmission = 0.6f;
    [SerializeField] private float _litEmission = 5f;

    [Header("Prompt")]
    [SerializeField] private string _prompt = "Light Brazier";

    public System.Action<Brazier> Activated;
    public int Index => _index;
    public void SetIndex(int i) => _index = i;
    public string Prompt => _prompt;

    bool _lit;
    bool _interactable;
    float _flicker = 1f;
    AudioSource _audio;
    AudioClip _tone;
    MaterialPropertyBlock _mpb;
    Coroutine _flashCo;
    Coroutine _revealCo;
    Vector3 _baseScale = Vector3.one;
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _baseScale = transform.localScale;
        EnsureFlame();
        SetLit(false);
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 1f;
        _audio.maxDistance = 18f;
        _audio.rolloffMode = AudioRolloffMode.Linear;
    }

    void EnsureFlame()
    {
        if (_flameLight == null)
        {
            var go = new GameObject("Flame");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * _flameHeight;
            _flameLight = go.AddComponent<Light>();
            _flameLight.type = LightType.Point;
            _flameLight.shadows = LightShadows.None;
        }
        _flameLight.color = _flameColor;
        _flameLight.range = _flameRange;
    }

    void Update()
    {
        if (_flameLight == null || !_lit) return;
        _flicker = Mathf.Lerp(_flicker, Random.Range(0.85f, 1.15f), Time.deltaTime * 10f);
        _flameLight.intensity = _litIntensity * _flicker;
    }

    public void SetInteractable(bool on) => _interactable = on;

    public void Interact()
    {
        if (!_interactable) return;
        Activated?.Invoke(this);
    }

    public void SetFlameColor(Color c)
    {
        _flameColor = c;
        EnsureFlame();
        if (_flameLight != null) _flameLight.color = c;
        ApplyGlow(_lit ? _litEmission : _idleEmission);
    }

    public void SetLit(bool lit)
    {
        if (_revealCo != null) { StopCoroutine(_revealCo); _revealCo = null; }
        _lit = lit;
        if (_flameLight != null) _flameLight.intensity = lit ? _litIntensity : 0f;
        if (_flameVisual != null) _flameVisual.SetActive(lit);
        ApplyGlow(lit ? _litEmission : _idleEmission);
    }

    void ApplyGlow(float intensity)
    {
        if (_glowRenderer == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _glowRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(EmissionColorId, _flameColor * intensity);
        _glowRenderer.SetPropertyBlock(_mpb);
    }

    public void Flash(float duration, bool playTone = true)
    {
        if (_revealCo != null) { StopCoroutine(_revealCo); _revealCo = null; }
        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(FlashRoutine(duration));
        if (playTone) PlayTone();
    }

    IEnumerator FlashRoutine(float duration)
    {
        SetLit(true);
        yield return new WaitForSeconds(duration);
        SetLit(false);
        _flashCo = null;
    }

    public void CancelFlash()
    {
        if (_flashCo != null) { StopCoroutine(_flashCo); _flashCo = null; }
    }

    public void Reveal(float duration, bool playTone = true)
    {
        gameObject.SetActive(true);
        if (_revealCo != null) StopCoroutine(_revealCo);
        _revealCo = StartCoroutine(RevealRoutine(Mathf.Max(0.05f, duration)));
        if (playTone) PlayTone();
    }

    IEnumerator RevealRoutine(float duration)
    {
        _lit = false;
        if (_flameLight != null) _flameLight.intensity = 0f;
        transform.localScale = _baseScale * 0.01f;
        ApplyGlow(0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            transform.localScale = _baseScale * Mathf.Lerp(0.01f, 1f, ease);
            ApplyGlow(_idleEmission * ease);
            yield return null;
        }

        transform.localScale = _baseScale;
        ApplyGlow(_idleEmission);
        _revealCo = null;
    }

    public void PlayTone()
    {
        if (_tone == null)
        {
            float f = 294f * Mathf.Pow(1.18f, Mathf.Max(0, _index));
            _tone = ProceduralSfx.Chime(f, f * 1.5f, 0.2f, 0.5f);
        }
        if (_audio != null) { _audio.pitch = 1f; _audio.PlayOneShot(_tone); }
    }
}
