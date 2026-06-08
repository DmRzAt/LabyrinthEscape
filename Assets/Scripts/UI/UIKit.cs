using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class UIKit
{
    public static readonly Color Dim       = new Color(0f, 0f, 0f, 0.80f);
    public static readonly Color Panel     = new Color(0.20f, 0.08f, 0.05f, 0.98f);
    public static readonly Color Edge      = new Color(0.90f, 0.45f, 0.12f, 1f);
    public static readonly Color TextCol   = new Color(0.96f, 0.94f, 0.90f, 1f);
    public static readonly Color Muted     = new Color(0.70f, 0.55f, 0.45f, 1f);
    public static readonly Color BtnNormal = new Color(0.05f, 0.04f, 0.035f, 1f);
    public static readonly Color BarBack   = new Color(0.16f, 0.17f, 0.21f, 1f);
    public static readonly Color BarFill   = new Color(0.90f, 0.45f, 0.12f, 1f);
    public static readonly Color BoxOff    = new Color(0.16f, 0.17f, 0.21f, 1f);

    public static TMP_FontAsset Font => TMP_Settings.defaultFontAsset;

    public static GameObject NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = TextCol;
        t.raycastTarget = false;
        return t;
    }

    public static GameObject Box(string name, Transform parent, Vector2 size)
    {
        var go = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        go.AddComponent<Image>().color = Panel;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Edge;
        outline.effectDistance = new Vector2(3f, -3f);
        return go;
    }

    public static void Title(Transform parent, string text, float y)
    {
        var label = Text(parent, text, 46f, FontStyles.Bold, TextAlignmentOptions.Center);
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(800f, 70f);
        label.color = Edge;
        label.characterSpacing = 12f;
    }

    public static Button Button(Transform parent, float x, float y, float width, float height, string text, System.Action onClick)
    {
        var go = NewRect("Btn_" + text, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, y), new Vector2(width, height));
        var img = go.AddComponent<Image>();
        img.color = BtnNormal;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Edge;
        outline.effectDistance = new Vector2(2f, -2f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.7f, 1.4f, 1.0f, 1f);
        colors.pressedColor = new Color(0.7f, 0.55f, 0.4f, 1f);
        colors.selectedColor = new Color(1.4f, 1.2f, 0.9f, 1f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(() => { PlayClick(); onClick(); });
        AddHoverSound(go);

        var label = Text(go.transform, text, 26f, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return btn;
    }

    public static void Header(Transform parent, string text, float x, float y, float width)
    {
        var label = Text(parent, text, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 26f);
        label.color = Edge;
        label.characterSpacing = 6f;

        var line = NewRect("HeaderLine", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, y - 18f), new Vector2(width, 2f));
        line.AddComponent<Image>().color = new Color(Edge.r, Edge.g, Edge.b, 0.5f);
    }

    public static GameObject Confirm(Transform parent, string message, System.Action onConfirm)
    {
        var root = NewRect("Confirm", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Stretch((RectTransform)root.transform);
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        root.transform.SetAsLastSibling();

        var box = Box("ConfirmBox", root.transform, new Vector2(620f, 260f));
        var label = Text(box.transform, message, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0.5f, 0.5f); lrt.anchorMax = new Vector2(0.5f, 0.5f); lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, 50f); lrt.sizeDelta = new Vector2(560f, 120f);

        Button(box.transform, -130f, -70f, 220f, 56f, "Yes", () => { Object.Destroy(root); onConfirm?.Invoke(); });
        var no = Button(box.transform, 130f, -70f, 220f, 56f, "No", () => Object.Destroy(root));
        SelectFirst(no.gameObject);
        return root;
    }

    public static void SelectFirst(GameObject target)
    {
        if (EventSystem.current == null || target == null) return;
        var sel = target.GetComponent<Selectable>();
        if (sel == null) sel = target.GetComponentInChildren<Selectable>(false);
        if (sel != null) EventSystem.current.SetSelectedGameObject(sel.gameObject);
    }

    static AudioSource _audio;
    static AudioClip _click, _hover;

    static void EnsureAudio()
    {
        if (_audio != null) return;
        var go = new GameObject("UIAudio");
        Object.DontDestroyOnLoad(go);
        _audio = go.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _click = MakeBeep(880f, 0.06f, 0.22f);
        _hover = MakeBeep(520f, 0.035f, 0.10f);
    }

    static AudioClip MakeBeep(float freq, float dur, float vol)
    {
        int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 32f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
        }
        var clip = AudioClip.Create("ui_beep", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void PlayClick() { EnsureAudio(); _audio.PlayOneShot(_click); }
    public static void PlayHover() { EnsureAudio(); _audio.PlayOneShot(_hover); }

    static void AddHoverSound(GameObject go)
    {
        var trigger = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => PlayHover());
        trigger.triggers.Add(enter);
        var select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        select.callback.AddListener(_ => PlayHover());
        trigger.triggers.Add(select);
    }
}
