using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    const string KEY_SHAKE = "opt_shakeStrength";
    const string KEY_HEADBOB = "opt_headbob";
    const string KEY_LOOKSMOOTH = "opt_lookSmooth";

    readonly List<Resolution> _resolutions = new List<Resolution>();
    Transform _panel;
    System.Action _onBack;

    public static GameObject Create(Transform parent, System.Action onBack)
    {
        var box = UIKit.Box("SettingsPanel", parent, new Vector2(1180f, 820f));
        var sp = box.AddComponent<SettingsPanel>();
        sp.Build(box.transform, onBack);
        return box;
    }

    void Build(Transform panel, System.Action onBack)
    {
        _panel = panel;
        _onBack = onBack;

        UIKit.Title(panel, "SETTINGS", 350f);

        const float lx = -300f, rx = 300f, top = 250f, step = 72f;

        UIKit.Header(panel, "GAMEPLAY & AUDIO", lx, top + 44f, 500f);
        UIKit.Header(panel, "GRAPHICS", rx, top + 44f, 500f);

        MakeSlider(panel, lx, top - 0 * step, "Volume", 0f, 1f,
            PlayerPrefs.GetFloat(GameSettings.KEY_VOLUME, 0.8f), v => $"Volume: {Mathf.RoundToInt(v * 100)}%",
            v => { AudioListener.volume = v; PlayerPrefs.SetFloat(GameSettings.KEY_VOLUME, v); });

        MakeSlider(panel, lx, top - 1 * step, "Sensitivity", 0.1f, 3f,
            PlayerPrefs.GetFloat(GameSettings.KEY_SENS, 1f), v => $"Sensitivity: {v:0.0}",
            v => PlayerPrefs.SetFloat(GameSettings.KEY_SENS, v));

        MakeSlider(panel, lx, top - 2 * step, "FOV", 60f, 100f,
            PlayerPrefs.GetFloat(GameSettings.KEY_FOV, GameSettings.DEFAULT_FOV), v => $"FOV: {Mathf.RoundToInt(v)}",
            v => GameSettings.SetFov(v));

        MakeSlider(panel, lx, top - 3 * step, "Camera Shake", 0f, 1f,
            PlayerPrefs.GetFloat(KEY_SHAKE, 1f), v => $"Camera Shake: {Mathf.RoundToInt(v * 100)}%",
            v => { if (CameraShake.Instance != null) CameraShake.Instance.SetStrength(v); else PlayerPrefs.SetFloat(KEY_SHAKE, v); });

        MakeSlider(panel, lx, top - 4 * step, "Look Smoothing", 0f, 0.2f,
            PlayerPrefs.GetFloat(KEY_LOOKSMOOTH, 0f), v => $"Look Smoothing: {v:0.00}",
            v => PlayerPrefs.SetFloat(KEY_LOOKSMOOTH, v));

        MakeToggle(panel, lx, top - 5 * step, "Invert X",
            PlayerPrefs.GetInt(GameSettings.KEY_INVX, 0) == 1,
            b => PlayerPrefs.SetInt(GameSettings.KEY_INVX, b ? 1 : 0));

        MakeToggle(panel, lx, top - 6 * step, "Invert Y",
            PlayerPrefs.GetInt(GameSettings.KEY_INVY, 0) == 1,
            b => PlayerPrefs.SetInt(GameSettings.KEY_INVY, b ? 1 : 0));

        MakeToggle(panel, lx, top - 7 * step, "Headbob",
            PlayerPrefs.GetInt(KEY_HEADBOB, 1) == 1,
            b => PlayerPrefs.SetInt(KEY_HEADBOB, b ? 1 : 0));

        BuildResolutionList();
        var resOpts = new List<string>();
        foreach (var r in _resolutions) resOpts.Add(r.width + "x" + r.height);
        int curResIdx = _resolutions.FindIndex(r =>
            r.width == PlayerPrefs.GetInt(GameSettings.KEY_RES_W, Screen.width) &&
            r.height == PlayerPrefs.GetInt(GameSettings.KEY_RES_H, Screen.height));
        MakeDropdown(panel, rx, top - 0 * step, "Resolution", resOpts, Mathf.Max(0, curResIdx), OnResolutionChanged);

        var qNames = new List<string>(QualitySettings.names);
        MakeDropdown(panel, rx, top - 1 * step, "Quality", qNames,
            PlayerPrefs.GetInt(GameSettings.KEY_QUALITY, QualitySettings.GetQualityLevel()),
            i => { QualitySettings.SetQualityLevel(i, true); PlayerPrefs.SetInt(GameSettings.KEY_QUALITY, i); });

        var texOpts = new List<string> { "Ultra", "High", "Medium", "Low" };
        MakeDropdown(panel, rx, top - 2 * step, "Texture", texOpts,
            Mathf.Clamp(PlayerPrefs.GetInt(GameSettings.KEY_TEXQ, 0), 0, 3),
            i => { QualitySettings.globalTextureMipmapLimit = i; PlayerPrefs.SetInt(GameSettings.KEY_TEXQ, i); });

        MakeToggle(panel, rx, top - 3 * step, "VSync",
            PlayerPrefs.GetInt(GameSettings.KEY_VSYNC, 1) == 1,
            b => { QualitySettings.vSyncCount = b ? 1 : 0; PlayerPrefs.SetInt(GameSettings.KEY_VSYNC, b ? 1 : 0); });

        MakeToggle(panel, rx, top - 4 * step, "Fullscreen",
            PlayerPrefs.GetInt(GameSettings.KEY_FULL, 1) == 1,
            b => ChangeDisplay(() => PlayerPrefs.SetInt(GameSettings.KEY_FULL, b ? 1 : 0)));

        UIKit.Button(panel, -180f, -350f, 300f, 60f, "Reset to Defaults", ConfirmReset);
        UIKit.Button(panel, 180f, -350f, 300f, 60f, "Back", () => onBack?.Invoke());
    }

    void ConfirmReset()
    {
        UIKit.Confirm(_panel.parent, "Reset all settings to defaults?", () =>
        {
            GameSettings.ResetToDefaults();
            Rebuild();
        });
    }

    void Rebuild()
    {
        var onBack = _onBack;
        for (int i = _panel.childCount - 1; i >= 0; i--)
            Destroy(_panel.GetChild(i).gameObject);
        Build(_panel, onBack);
    }

    void BuildResolutionList()
    {
        _resolutions.Clear();
        var seen = new HashSet<string>();
        foreach (var r in Screen.resolutions)
        {
            string key = r.width + "x" + r.height;
            if (seen.Add(key)) _resolutions.Add(r);
        }
    }

    void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _resolutions.Count) return;
        var r = _resolutions[index];
        ChangeDisplay(() =>
        {
            PlayerPrefs.SetInt(GameSettings.KEY_RES_W, r.width);
            PlayerPrefs.SetInt(GameSettings.KEY_RES_H, r.height);
        });
    }

    void ChangeDisplay(System.Action applyPrefs)
    {
        int pw = Screen.width, ph = Screen.height;
        var pmode = Screen.fullScreenMode;
        bool hadRes = PlayerPrefs.HasKey(GameSettings.KEY_RES_W);
        int oldW = PlayerPrefs.GetInt(GameSettings.KEY_RES_W, pw);
        int oldH = PlayerPrefs.GetInt(GameSettings.KEY_RES_H, ph);
        int oldFull = PlayerPrefs.GetInt(GameSettings.KEY_FULL, 1);

        applyPrefs();
        ApplyResolution();

        System.Action revert = () =>
        {
            if (hadRes) { PlayerPrefs.SetInt(GameSettings.KEY_RES_W, oldW); PlayerPrefs.SetInt(GameSettings.KEY_RES_H, oldH); }
            else { PlayerPrefs.DeleteKey(GameSettings.KEY_RES_W); PlayerPrefs.DeleteKey(GameSettings.KEY_RES_H); }
            PlayerPrefs.SetInt(GameSettings.KEY_FULL, oldFull);
            Screen.SetResolution(pw, ph, pmode);
            Rebuild();
        };
        ShowRevertDialog(revert);
    }

    void ShowRevertDialog(System.Action revert)
    {
        var root = UIKit.NewRect("RevertConfirm", _panel.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)root.transform);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        root.transform.SetAsLastSibling();

        var box = UIKit.Box("RevertBox", root.transform, new Vector2(640f, 240f));
        var label = UIKit.Text(box.transform, "", 26f, TMPro.FontStyles.Bold, TMPro.TextAlignmentOptions.Center);
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f); lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, 50f); lrt.sizeDelta = new Vector2(580f, 110f);

        UIKit.Button(box.transform, -130f, -60f, 220f, 56f, "Keep", () => Destroy(root));
        var revertBtn = UIKit.Button(box.transform, 130f, -60f, 220f, 56f, "Revert", () => { Destroy(root); revert(); });
        UIKit.SelectFirst(revertBtn.gameObject);

        StartCoroutine(RevertCountdown(label, root, 10f, revert));
    }

    IEnumerator RevertCountdown(TMPro.TextMeshProUGUI label, GameObject root, float seconds, System.Action revert)
    {
        float t = seconds;
        while (t > 0f)
        {
            if (root == null) yield break;
            label.text = $"Keep these display settings?\nReverting in {Mathf.CeilToInt(t)}...";
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (root != null) { Destroy(root); revert(); }
    }

    void ApplyResolution()
    {
        bool full = PlayerPrefs.GetInt(GameSettings.KEY_FULL, 1) == 1;
        var mode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (PlayerPrefs.HasKey(GameSettings.KEY_RES_W) && PlayerPrefs.HasKey(GameSettings.KEY_RES_H))
            Screen.SetResolution(PlayerPrefs.GetInt(GameSettings.KEY_RES_W), PlayerPrefs.GetInt(GameSettings.KEY_RES_H), mode);
        else
            Screen.fullScreenMode = mode;
    }

    void MakeSlider(Transform parent, float x, float y, string title, float min, float max, float value,
        System.Func<float, string> fmt, System.Action<float> onChange)
    {
        var row = UIKit.NewRect("Slider_" + title, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, y), new Vector2(500f, 56f));

        var label = UIKit.Text(row.transform, fmt(value), 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f); lrt.pivot = new Vector2(0.5f, 1f);
        lrt.anchoredPosition = Vector2.zero; lrt.sizeDelta = new Vector2(0f, 26f);

        var sgo = UIKit.NewRect("S", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var srt = (RectTransform)sgo.transform;
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 6f); srt.sizeDelta = new Vector2(0f, 16f);
        sgo.AddComponent<Image>().color = UIKit.BarBack;

        var fillArea = UIKit.NewRect("Fill Area", sgo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)fillArea.transform);
        var fill = UIKit.NewRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)fill.transform);
        fill.AddComponent<Image>().color = UIKit.BarFill;

        var handle = UIKit.NewRect("Handle", sgo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 26f));
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = UIKit.TextCol;

        var slider = sgo.AddComponent<Slider>();
        slider.fillRect = (RectTransform)fill.transform;
        slider.handleRect = (RectTransform)handle.transform;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => { label.text = fmt(v); onChange(v); });
    }

    void MakeToggle(Transform parent, float x, float y, string title, bool value, System.Action<bool> onChange)
    {
        var row = UIKit.NewRect("Toggle_" + title, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, y), new Vector2(500f, 40f));

        var label = UIKit.Text(row.transform, title, 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.5f); lrt.anchorMax = new Vector2(0.7f, 0.5f); lrt.pivot = new Vector2(0f, 0.5f);
        lrt.offsetMin = new Vector2(0f, -18f); lrt.offsetMax = new Vector2(0f, 18f);

        var box = UIKit.NewRect("Box", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(32f, 32f));
        var boxImg = box.AddComponent<Image>();
        boxImg.color = UIKit.BoxOff;
        var outline = box.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var check = UIKit.NewRect("Check", box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        var checkImg = check.AddComponent<Image>();
        checkImg.color = UIKit.Edge;

        var toggle = box.AddComponent<Toggle>();
        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;
        toggle.SetIsOnWithoutNotify(value);
        toggle.onValueChanged.AddListener(b => onChange(b));
    }

    void MakeDropdown(Transform parent, float x, float y, string title, List<string> options, int value, System.Action<int> onChange)
    {
        var row = UIKit.NewRect("Dropdown_" + title, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, y), new Vector2(500f, 44f));

        var label = UIKit.Text(row.transform, title, 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.5f); lrt.anchorMax = new Vector2(0.45f, 0.5f); lrt.pivot = new Vector2(0f, 0.5f);
        lrt.offsetMin = new Vector2(0f, -20f); lrt.offsetMax = new Vector2(0f, 20f);

        var ddGo = UIKit.NewRect("DD", row.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-135f, 0f), new Vector2(270f, 40f));
        var ddImg = ddGo.AddComponent<Image>();
        ddImg.color = UIKit.BtnNormal;
        var ddOutline = ddGo.AddComponent<Outline>();
        ddOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        ddOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var dd = ddGo.AddComponent<TMP_Dropdown>();
        dd.targetGraphic = ddImg;

        var caption = UIKit.Text(ddGo.transform, "", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        var crt = caption.rectTransform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(12f, 0f); crt.offsetMax = new Vector2(-28f, 0f);
        dd.captionText = caption;

        var arrow = UIKit.Text(ddGo.transform, "▼", 14f, FontStyles.Bold, TextAlignmentOptions.Right);
        var art = arrow.rectTransform;
        art.anchorMin = new Vector2(1f, 0.5f); art.anchorMax = new Vector2(1f, 0.5f); art.pivot = new Vector2(1f, 0.5f);
        art.anchoredPosition = new Vector2(-10f, 0f); art.sizeDelta = new Vector2(20f, 20f);
        arrow.color = UIKit.Edge;

        var template = UIKit.NewRect("Template", ddGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
        var trt = (RectTransform)template.transform;
        trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -2f); trt.sizeDelta = new Vector2(0f, 200f);
        var tImg = template.AddComponent<Image>();
        tImg.color = new Color(0.10f, 0.05f, 0.04f, 1f);
        var tOutline = template.AddComponent<Outline>();
        tOutline.effectColor = UIKit.Edge; tOutline.effectDistance = new Vector2(1.5f, -1.5f);
        var sr = template.AddComponent<ScrollRect>();

        var viewport = UIKit.NewRect("Viewport", template.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var vprt = (RectTransform)viewport.transform;
        vprt.anchorMin = Vector2.zero; vprt.anchorMax = Vector2.one; vprt.pivot = new Vector2(0f, 1f);
        vprt.offsetMin = Vector2.zero; vprt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = UIKit.NewRect("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var cort = (RectTransform)content.transform;
        cort.anchorMin = new Vector2(0f, 1f); cort.anchorMax = new Vector2(1f, 1f); cort.pivot = new Vector2(0.5f, 1f);
        cort.anchoredPosition = Vector2.zero; cort.sizeDelta = new Vector2(0f, 36f);

        var item = UIKit.NewRect("Item", content.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
        var irt = (RectTransform)item.transform;
        irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(1f, 0.5f); irt.pivot = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(0f, 36f);
        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = UIKit.NewRect("Item Background", item.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)itemBg.transform);
        var itemBgImg = itemBg.AddComponent<Image>();
        itemBgImg.color = new Color(0.20f, 0.10f, 0.06f, 0f);

        var itemChk = UIKit.NewRect("Item Checkmark", item.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)itemChk.transform);
        var itemChkImg = itemChk.AddComponent<Image>();
        itemChkImg.color = new Color(0.55f, 0.27f, 0.08f, 0.9f);

        var itemLabel = UIKit.Text(item.transform, "Option", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        var ilrt = itemLabel.rectTransform;
        ilrt.anchorMin = Vector2.zero; ilrt.anchorMax = Vector2.one;
        ilrt.offsetMin = new Vector2(14f, 0f); ilrt.offsetMax = new Vector2(-6f, 0f);

        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = itemChkImg;
        itemToggle.isOn = false;

        sr.content = cort;
        sr.viewport = vprt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 20f;

        template.SetActive(false);
        dd.template = trt;
        dd.itemText = itemLabel;
        dd.ClearOptions();
        dd.AddOptions(options);
        dd.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, options.Count - 1)));
        dd.RefreshShownValue();
        dd.onValueChanged.AddListener(i => onChange(i));
    }
}
