using UnityEngine;
using UnityEngine.UI;

public class Hitmarker : MonoBehaviour
{
    static Hitmarker _instance;
    RectTransform _root;
    CanvasGroup _group;
    float _life, _dur;

    static AudioSource _audio;
    static AudioClip _tick, _killTick;

    static Hitmarker Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("Hitmarker");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Hitmarker>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static void Flash(bool kill) => Instance.Show(kill);

    void Build()
    {
        var canvasGO = new GameObject("Hitmarker_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var rootGO = new GameObject("Marker", typeof(RectTransform));
        rootGO.transform.SetParent(canvasGO.transform, false);
        _root = (RectTransform)rootGO.transform;
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition = Vector2.zero;
        _root.sizeDelta = new Vector2(40f, 40f);
        _group = rootGO.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        MakeTick(45f); MakeTick(135f); MakeTick(225f); MakeTick(315f);
    }

    void MakeTick(float angle)
    {
        var go = new GameObject("Tick", typeof(RectTransform));
        go.transform.SetParent(_root, false);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(3f, 11f);
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        rt.localRotation = rot;
        rt.anchoredPosition = rot * Vector3.up * 13f;
    }

    void Show(bool kill)
    {
        _dur = _life = 0.22f;
        _group.alpha = 1f;
        var c = kill ? new Color(1f, 0.25f, 0.2f) : Color.white;
        foreach (var img in _root.GetComponentsInChildren<Image>()) img.color = c;
        _root.localScale = Vector3.one * (kill ? 1.5f : 1.15f);
        PlayTick(kill);
    }

    void Update()
    {
        if (_group == null || _group.alpha <= 0f) return;
        _life -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_life / _dur);
        _group.alpha = k;
        _root.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, k);
        if (_life <= 0f) _group.alpha = 0f;
    }

    static void PlayTick(bool kill)
    {
        EnsureAudio();
        _audio.PlayOneShot(kill ? _killTick : _tick);
    }

    static void EnsureAudio()
    {
        if (_audio != null) return;
        var go = new GameObject("HitmarkerAudio");
        DontDestroyOnLoad(go);
        _audio = go.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _tick = MakeBeep(1500f, 0.035f, 0.22f);
        _killTick = MakeBeep(650f, 0.09f, 0.30f);
    }

    static AudioClip MakeBeep(float freq, float dur, float vol)
    {
        int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
        }
        var clip = AudioClip.Create("hitmarker", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
