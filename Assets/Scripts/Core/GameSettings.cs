using UnityEngine;

public static class GameSettings
{
    public const string KEY_VOLUME = "opt_volume";
    public const string KEY_SENS    = "opt_sensitivity";
    public const string KEY_FULL    = "opt_fullscreen";
    public const string KEY_INVX    = "opt_invertX";
    public const string KEY_INVY    = "opt_invertY";
    public const string KEY_FOV     = "opt_fov";
    public const string KEY_RES_W   = "opt_resW";
    public const string KEY_RES_H   = "opt_resH";
    public const string KEY_QUALITY = "opt_quality";
    public const string KEY_VSYNC   = "opt_vsync";
    public const string KEY_TEXQ    = "opt_texq";

    public const float DEFAULT_FOV = 80f;
    public const float DEFAULT_VOLUME = 0.8f;
    public const float DEFAULT_SENS = 1f;

    public static event System.Action<float> FovChanged;

    public static void ResetToDefaults()
    {
        string[] keys =
        {
            KEY_VOLUME, KEY_SENS, KEY_FULL, KEY_INVX, KEY_INVY, KEY_FOV,
            KEY_RES_W, KEY_RES_H, KEY_QUALITY, KEY_VSYNC, KEY_TEXQ,
            "opt_shakeStrength", "opt_headbob", "opt_lookSmooth"
        };
        foreach (var k in keys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();

        AudioListener.volume = DEFAULT_VOLUME;
        QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = 1;
        QualitySettings.globalTextureMipmapLimit = 0;
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        if (CameraShake.Instance != null) CameraShake.Instance.strength = 1f;
        FovChanged?.Invoke(DEFAULT_FOV);
    }

    public static float Fov => PlayerPrefs.GetFloat(KEY_FOV, DEFAULT_FOV);

    public static void SetFov(float v)
    {
        PlayerPrefs.SetFloat(KEY_FOV, v);
        FovChanged?.Invoke(v);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnLaunch()
    {
        ApplyGraphics();
        ApplyAudio();
    }

    public static void ApplyAudio()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
    }

    public static void ApplyGraphics()
    {
        if (PlayerPrefs.HasKey(KEY_QUALITY))
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(KEY_QUALITY), 0, QualitySettings.names.Length - 1), true);

        QualitySettings.vSyncCount = PlayerPrefs.GetInt(KEY_VSYNC, 1);
        QualitySettings.globalTextureMipmapLimit = PlayerPrefs.GetInt(KEY_TEXQ, 0);

        bool full = PlayerPrefs.GetInt(KEY_FULL, 1) == 1;
        var mode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (PlayerPrefs.HasKey(KEY_RES_W) && PlayerPrefs.HasKey(KEY_RES_H))
            Screen.SetResolution(PlayerPrefs.GetInt(KEY_RES_W), PlayerPrefs.GetInt(KEY_RES_H), mode);
        else
            Screen.fullScreenMode = mode;
    }
}
