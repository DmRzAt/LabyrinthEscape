using UnityEngine;

public static class ProceduralSfx
{
    const int Rate = 44100;

    public static AudioClip Footstep(int seed)
    {
        float dur = 0.18f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        var rng = new System.Random(seed);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 36f);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp = Mathf.Lerp(lp, white, 0.32f);
            float thump = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 55f) * 0.5f;
            data[i] = (lp * 0.6f + thump) * env * 0.5f;
        }
        return Make("step", data);
    }

    public static AudioClip Chime(float f1, float f2, float dur, float vol)
    {
        int n = (int)(Rate * dur);
        var data = new float[n];
        int half = n / 2;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = i < half ? f1 : f2;
            float env = Mathf.Exp(-((float)i % half) / Rate * 14f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * vol;
        }
        return Make("chime", data);
    }

    public static AudioClip Drip(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.35f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float f0 = 1400f + (float)rng.NextDouble() * 600f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float f = Mathf.Lerp(f0, f0 * 0.45f, t / dur);
            float env = Mathf.Exp(-t * 22f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.4f;
        }
        return Make("drip", data);
    }

    public static AudioClip Rumble(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 2.2f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float f = 28f + (float)rng.NextDouble() * 14f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float swell = Mathf.Sin(Mathf.PI * t / dur);
            float wob = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 0.7f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t * wob) * swell * 0.5f;
        }
        return Make("rumble", data);
    }

    public static AudioClip Creak(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.9f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Sin(Mathf.PI * t / dur);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp = Mathf.Lerp(lp, white, 0.06f);
            float wob = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 6f * t);
            data[i] = lp * env * wob * 0.35f;
        }
        return Make("creak", data);
    }

    public static AudioClip Clang(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.45f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float[] partials = { 2100f, 3170f, 4400f, 5300f, 6600f };
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 9f);
            float s = 0f;
            foreach (var f in partials) s += Mathf.Sin(2f * Mathf.PI * f * t);
            s /= partials.Length;
            float transient = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 120f) * 0.5f;
            data[i] = (s + transient) * env * 0.4f;
        }
        return Make("clang", data);
    }

    public static AudioClip Thud(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.20f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 30f);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp = Mathf.Lerp(lp, white, 0.12f);
            float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 40f) * 0.6f;
            data[i] = (lp * 0.5f + thump) * env * 0.5f;
        }
        return Make("thud", data);
    }

    public static AudioClip Jump(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.16f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Sin(Mathf.PI * t / dur);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp = Mathf.Lerp(lp, white, 0.18f);
            data[i] = lp * env * 0.3f;
        }
        return Make("jump", data);
    }

    public static AudioClip Land(int seed)
    {
        var rng = new System.Random(seed);
        float dur = 0.24f;
        int n = (int)(Rate * dur);
        var data = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * 26f);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp = Mathf.Lerp(lp, white, 0.22f);
            float thump = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-t * 34f) * 0.7f;
            data[i] = (lp * 0.5f + thump) * env * 0.6f;
        }
        return Make("land", data);
    }

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
