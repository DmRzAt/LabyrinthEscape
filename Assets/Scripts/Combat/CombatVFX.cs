using UnityEngine;

public static class CombatVFX
{
    static Material _mat;
    static HitVFXPool _pool;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _pool = null;
        _mat = null;
    }

    static Material SparkMat()
    {
        if (_mat == null)
        {
            _mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            _mat.SetFloat("_Surface", 1f);
            _mat.SetFloat("_Blend", 2f);
            _mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _mat.SetFloat("_ZWrite", 0f);
            _mat.renderQueue = 3100;
            _mat.SetColor("_BaseColor", Color.white);
        }
        return _mat;
    }

    static HitVFXPool Pool()
    {
        if (_pool == null)
        {
            var go = new GameObject("HitVFXPool");
            Object.DontDestroyOnLoad(go);
            _pool = go.AddComponent<HitVFXPool>();
            _pool.Init(SparkMat(), 8);
        }
        return _pool;
    }

    public static void SpawnHit(Vector3 pos, Vector3 normal, float intensity = 1f)
    {
        Pool().Play(pos, normal, intensity);
    }
}

class HitVFXPool : MonoBehaviour
{
    class Entry
    {
        public ParticleSystem ps;
        public Light light;
        public HitFlashLight flash;
    }

    Entry[] _entries;
    int _next;

    public void Init(Material sparkMat, int count)
    {
        _entries = new Entry[count];
        for (int i = 0; i < count; i++) _entries[i] = Build(sparkMat);
    }

    Entry Build(Material sparkMat)
    {
        var go = new GameObject("HitVFX");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(1.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.playOnAwake = false;

        var em = ps.emission;
        em.rateOverTime = 0f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 55f;
        sh.radius = 0.03f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f), new GradientColorKey(new Color(0.8f, 0.7f, 0.55f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = sparkMat;
        pr.renderMode = ParticleSystemRenderMode.Stretch;
        pr.velocityScale = 0.06f;
        pr.lengthScale = 2f;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pr.receiveShadows = false;
        pr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        var lgo = new GameObject("ImpactFlash");
        lgo.transform.SetParent(go.transform, false);
        var light = lgo.AddComponent<Light>();
        light.type = LightType.Point;
        light.shadows = LightShadows.None;
        light.enabled = false;
        var flash = lgo.AddComponent<HitFlashLight>();

        return new Entry { ps = ps, light = light, flash = flash };
    }

    public void Play(Vector3 pos, Vector3 normal, float intensity)
    {
        if (_entries == null || _entries.Length == 0) return;
        intensity = Mathf.Clamp(intensity, 0.5f, 2.5f);

        Entry e = _entries[_next];
        _next = (_next + 1) % _entries.Length;
        if (e.ps == null) return;

        e.ps.transform.position = pos;
        e.ps.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);

        Color hot = Color.Lerp(new Color(1f, 0.95f, 0.78f), new Color(1f, 0.55f, 0.25f), Mathf.InverseLerp(1f, 2f, intensity));

        var main = e.ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * intensity, 6.5f * intensity);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f * intensity);
        main.startColor = new ParticleSystem.MinMaxGradient(hot, new Color(0.85f, 0.82f, 0.7f, 1f));

        var em = e.ps.emission;
        int lo = Mathf.RoundToInt(14 * intensity), hi = Mathf.RoundToInt(20 * intensity);
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)lo, (short)hi) });

        if (e.light != null)
        {
            e.light.color = hot;
            e.light.intensity = 4.5f * intensity;
            e.light.range = 3.2f * intensity;
            if (e.flash != null) e.flash.Init(0.12f);
        }

        e.ps.Clear();
        e.ps.Play();
    }
}

class HitFlashLight : MonoBehaviour
{
    Light _light;
    float _dur, _life, _start;
    bool _active;

    public void Init(float dur)
    {
        if (_light == null) _light = GetComponent<Light>();
        _dur = Mathf.Max(0.001f, dur);
        _life = _dur;
        _start = _light != null ? _light.intensity : 0f;
        if (_light != null) _light.enabled = true;
        _active = true;
    }

    void Update()
    {
        if (!_active) return;
        if (_light == null) { _active = false; return; }
        _life -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_life / _dur);
        _light.intensity = _start * k;
        if (_life <= 0f) { _light.enabled = false; _active = false; }
    }
}
