using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 0.7f;
    [Tooltip("Distance walked between footstep sounds.")]
    public float stride = 1.7f;

    const int Rate = 44100;
    const float PI2 = 6.2831853f;

    AudioSource _src;

    static AudioClip s_alert, s_attack, s_hurt, s_death, s_step;
    static bool s_built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSharedClips()
    {
        s_built = false;
        s_alert = s_attack = s_hurt = s_death = s_step = null;
    }

    static void EnsureClipsBuilt()
    {
        if (s_built && s_alert != null) return;
        s_alert  = BuildGrowl();
        s_attack = BuildWhoosh();
        s_hurt   = BuildGrunt();
        s_death  = BuildGroan();
        s_step   = BuildStep();
        s_built  = true;
    }

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 1f;
        _src.rolloffMode = AudioRolloffMode.Logarithmic;
        _src.minDistance = 2f;
        _src.maxDistance = 22f;
        _src.dopplerLevel = 0f;

        EnsureClipsBuilt();
    }

    void Play(AudioClip c, float vol, float jitter)
    {
        if (c == null || _src == null) return;
        _src.pitch = 1f + Random.Range(-jitter, jitter);
        _src.PlayOneShot(c, vol * volume);
    }

    public void PlayAlert()  => Play(s_alert,  1.0f, 0.06f);
    public void PlayAttack() => Play(s_attack, 0.8f, 0.10f);
    public void PlayHurt()   => Play(s_hurt,   0.9f, 0.12f);
    public void PlayDeath()  => Play(s_death,  1.0f, 0.05f);
    public void PlayStep()   => Play(s_step,   0.35f, 0.15f);

    static AudioClip Make(string name, float[] d)
    {
        Fade(d, 96);
        var c = AudioClip.Create(name, d.Length, 1, Rate, false);
        c.SetData(d, 0);
        return c;
    }

    static void Fade(float[] d, int n)
    {
        n = Mathf.Min(n, d.Length / 2);
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            d[i] *= k;
            d[d.Length - 1 - i] *= k;
        }
    }

    static float Saw(float phase) => 2f * (phase - Mathf.Floor(phase + 0.5f));
    static float Noise() => Random.value * 2f - 1f;

    static AudioClip BuildGrowl()
    {
        int n = (int)(Rate * 0.55f);
        var d = new float[n];
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = 95f + 10f * Mathf.Sin(PI2 * 5f * t);
            phase += f / Rate;
            float x = Saw(phase) * 0.7f + Noise() * 0.3f;
            lp += 0.18f * (x - lp);
            float env = Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t * 3.2f);
            d[i] = lp * env * 0.8f;
        }
        return Make("growl", d);
    }

    static AudioClip BuildWhoosh()
    {
        int n = (int)(Rate * 0.3f);
        var d = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float p = (float)i / n;
            float a = 0.05f + 0.5f * Mathf.Sin(p * Mathf.PI);
            lp += a * (Noise() - lp);
            float env = Mathf.Sin(p * Mathf.PI);
            d[i] = lp * env * env * 0.9f;
        }
        return Make("whoosh", d);
    }

    static AudioClip BuildGrunt()
    {
        int n = (int)(Rate * 0.2f);
        var d = new float[n];
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            phase += 140f / Rate;
            float x = Mathf.Sin(PI2 * phase) * 0.6f + Noise() * 0.6f;
            lp += 0.25f * (x - lp);
            float env = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * 14f);
            d[i] = lp * env * 0.85f;
        }
        return Make("grunt", d);
    }

    static AudioClip BuildGroan()
    {
        int n = (int)(Rate * 0.85f);
        var d = new float[n];
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float p = t / 0.85f;
            float f = Mathf.Lerp(150f, 55f, p);
            phase += f / Rate;
            float x = Saw(phase) * 0.7f + Noise() * 0.25f;
            lp += 0.16f * (x - lp);
            float env = Mathf.Min(1f, t / 0.04f) * Mathf.Exp(-t * 2.2f);
            d[i] = lp * env * 0.85f;
        }
        return Make("groan", d);
    }

    static AudioClip BuildStep()
    {
        int n = (int)(Rate * 0.09f);
        var d = new float[n];
        float lp = 0f, phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            phase += 70f / Rate;
            float x = Noise() * 0.6f + Mathf.Sin(PI2 * phase) * 0.5f;
            lp += 0.12f * (x - lp);
            d[i] = lp * Mathf.Exp(-t * 45f) * 0.9f;
        }
        return Make("step", d);
    }
}
