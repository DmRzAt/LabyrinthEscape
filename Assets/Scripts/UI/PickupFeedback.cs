using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupFeedback : MonoBehaviour
{
    static PickupFeedback _instance;
    TextMeshProUGUI _label;
    CanvasGroup _group;
    float _life, _dur;

    static AudioSource _audio;
    static AudioClip _itemChime, _keyChime;

    static PickupFeedback Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PickupFeedback");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PickupFeedback>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static void Show(string itemName, bool isKey) => Instance.ShowToast(itemName, isKey);

    public static void ShowMessage(string text, float seconds = 1.8f) => Instance.ShowRaw(text, seconds);

    void ShowRaw(string text, float seconds)
    {
        _label.text = text;
        _label.color = UIKit.TextCol;
        _dur = _life = Mathf.Max(0.6f, seconds);
        _group.alpha = 1f;
    }

    void Build()
    {
        var canvasGO = new GameObject("Pickup_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var go = new GameObject("Toast", typeof(RectTransform));
        go.transform.SetParent(canvasGO.transform, false);
        _group = go.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _label = go.AddComponent<TextMeshProUGUI>();
        if (UIKit.Font != null) _label.font = UIKit.Font;
        _label.fontSize = 30f;
        _label.fontStyle = FontStyles.Bold;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = UIKit.TextCol;
        _label.raycastTarget = false;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        var rt = _label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -160f);
        rt.sizeDelta = new Vector2(900f, 44f);
        _label.outlineWidth = 0.2f;
        _label.outlineColor = new Color(0f, 0f, 0f, 0.9f);
    }

    void ShowToast(string itemName, bool isKey)
    {
        _label.text = isKey ? $"Key +1" : $"Picked up: {itemName}";
        _label.color = isKey ? UIKit.Edge : UIKit.TextCol;
        _dur = _life = 1.8f;
        _group.alpha = 1f;
        PlayChime(isKey);
    }

    void Update()
    {
        if (_group == null || _group.alpha <= 0f) return;
        _life -= Time.unscaledDeltaTime;
        _group.alpha = Mathf.Clamp01(_life / 0.6f);
        if (_life <= 0f) _group.alpha = 0f;
    }

    static void PlayChime(bool isKey)
    {
        EnsureAudio();
        _audio.PlayOneShot(isKey ? _keyChime : _itemChime);
    }

    static void EnsureAudio()
    {
        if (_audio != null) return;
        var go = new GameObject("PickupAudio");
        DontDestroyOnLoad(go);
        _audio = go.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _itemChime = ProceduralSfx.Chime(660f, 990f, 0.16f, 0.28f);
        _keyChime  = ProceduralSfx.Chime(880f, 1320f, 0.20f, 0.30f);
    }
}
