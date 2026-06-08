using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostFXPunch : MonoBehaviour
{
    static PostFXPunch _instance;
    Volume _volume;
    Vignette _vignette;
    ChromaticAberration _chroma;
    float _life, _dur, _strength;

    static PostFXPunch Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PostFXPunch");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PostFXPunch>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static void Punch(float strength) => Instance.Begin(strength);

    void Build()
    {
        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 100f;
        _volume.weight = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.profile = profile;

        _vignette = profile.Add<Vignette>(true);
        _vignette.intensity.overrideState = true;
        _vignette.color.overrideState = true;
        _vignette.color.value = new Color(0.45f, 0.02f, 0.02f);

        _chroma = profile.Add<ChromaticAberration>(true);
        _chroma.intensity.overrideState = true;
    }

    void Begin(float strength)
    {
        _strength = Mathf.Clamp(strength, 0.2f, 1.5f);
        _dur = _life = 0.25f;
    }

    void Update()
    {
        if (_volume == null) return;
        if (_life <= 0f)
        {
            if (_volume.weight > 0f) _volume.weight = 0f;
            return;
        }
        _life -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_life / _dur);
        _volume.weight = k;
        _vignette.intensity.value = 0.45f * _strength * k;
        _chroma.intensity.value = 0.6f * _strength * k;
    }
}
