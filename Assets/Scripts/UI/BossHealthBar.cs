using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    static readonly Color ColPanel = new Color(0.02f, 0.015f, 0.012f, 0.55f);
    static readonly Color ColBack = new Color(0.015f, 0.012f, 0.01f, 0.95f);
    static readonly Color ColFillHigh = new Color(0.85f, 0.10f, 0.08f, 1f);
    static readonly Color ColFillLow = new Color(0.45f, 0.02f, 0.02f, 1f);
    static readonly Color ColText = new Color(0.96f, 0.88f, 0.72f, 1f);

    static BossHealthBar _instance;

    EnemyHealth _boss;
    CanvasGroup _group;
    RectTransform _fill;
    Image _fillImage;
    TextMeshProUGUI _nameText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _instance = null;

    public static void Register(EnemyHealth boss, string displayName)
    {
        if (boss == null) return;
        if (_instance == null)
        {
            var go = new GameObject("BossHealthBar");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<BossHealthBar>();
            _instance.Build();
        }
        _instance._boss = boss;
        if (_instance._nameText != null)
            _instance._nameText.text = string.IsNullOrEmpty(displayName) ? "BOSS" : displayName.ToUpperInvariant();
        _instance._group.alpha = 0f;
    }

    void Build()
    {
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("BossPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -28f);
        prt.sizeDelta = new Vector2(760f, 64f);

        _group = panel.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var bgPanel = panel.AddComponent<Image>();
        bgPanel.color = ColPanel;
        bgPanel.raycastTarget = false;

        _nameText = NewText(panel.transform, "BOSS");
        var nrt = _nameText.rectTransform;
        nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.pivot = new Vector2(0.5f, 1f);
        nrt.sizeDelta = new Vector2(-24f, 26f);
        nrt.anchoredPosition = new Vector2(0f, -4f);
        _nameText.fontSize = 22f;
        _nameText.alignment = TextAlignmentOptions.Center;

        var back = new GameObject("BarBack", typeof(RectTransform));
        back.transform.SetParent(panel.transform, false);
        var backImg = back.AddComponent<Image>();
        backImg.color = ColBack;
        backImg.raycastTarget = false;
        var brt = (RectTransform)back.transform;
        brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(-24f, 20f);
        brt.anchoredPosition = new Vector2(0f, 10f);
        var outline = back.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(back.transform, false);
        _fillImage = fillGO.AddComponent<Image>();
        _fillImage.color = ColFillHigh;
        _fillImage.raycastTarget = false;
        _fill = _fillImage.rectTransform;
        _fill.anchorMin = Vector2.zero; _fill.anchorMax = Vector2.one;
        _fill.offsetMin = new Vector2(2f, 2f); _fill.offsetMax = new Vector2(-2f, -2f);
    }

    static TextMeshProUGUI NewText(Transform parent, string text)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontStyle = FontStyles.Bold;
        t.color = ColText;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    void Update()
    {
        if (_group == null) return;

        bool show = _boss != null && !_boss.IsDead;
        _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, Time.deltaTime * 4f);

        if (!show)
        {
            if (_boss != null && _boss.IsDead) _boss = null;
            return;
        }

        float ratio = Mathf.Clamp01((float)_boss.currentHP / Mathf.Max(1, _boss.maxHP));
        if (_fill != null) _fill.anchorMax = new Vector2(ratio, 1f);
        if (_fillImage != null) _fillImage.color = Color.Lerp(ColFillLow, ColFillHigh, ratio);
    }
}
