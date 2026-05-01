using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChestUI : MonoBehaviour
{
    public static ChestUI Instance { get; private set; }

    GameObject _root;
    GameObject _panel;
    Transform _itemsContent;
    Chest _current;
    PlayerController _player;

    static readonly Color ColBg     = new Color(0.10f, 0.08f, 0.06f, 0.97f);
    static readonly Color ColHeader = new Color(0.18f, 0.13f, 0.08f, 1f);
    static readonly Color ColAccent = new Color(0.85f, 0.65f, 0.30f, 1f);
    static readonly Color ColBtn    = new Color(0.22f, 0.18f, 0.13f, 1f);
    static readonly Color ColBtnHi  = new Color(0.36f, 0.28f, 0.18f, 1f);
    static readonly Color ColBtnDn  = new Color(0.45f, 0.34f, 0.20f, 1f);
    static readonly Color ColText   = new Color(0.95f, 0.90f, 0.78f, 1f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("ChestUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _root = NewGO("Root", canvasGO.transform);
        var dim = _root.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.6f);
        Stretch(_root.GetComponent<RectTransform>());

        _panel = NewGO("Panel", _root.transform);
        var bg = _panel.AddComponent<Image>();
        bg.color = ColBg;
        var prt = _panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(680, 780);
        prt.anchoredPosition = Vector2.zero;
        var outline = _panel.AddComponent<Outline>();
        outline.effectColor = ColAccent;
        outline.effectDistance = new Vector2(3, -3);

        var header = NewGO("Header", _panel.transform);
        header.AddComponent<Image>().color = ColHeader;
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1);
        hrt.anchorMax = new Vector2(1, 1);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0, 90);
        hrt.anchoredPosition = Vector2.zero;

        var title = MakeText(header.transform, "Title", "CHEST", 52, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = ColAccent;
        Stretch(title.rectTransform);

        var content = NewGO("Items", _panel.transform);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 0);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.offsetMin = new Vector2(30, 120);
        crt.offsetMax = new Vector2(-30, -110);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        _itemsContent = content.transform;

        MakeRectButton(_panel.transform, "TakeAll", "TAKE ALL",
            new Vector2(0.5f, 0f), new Vector2(-150, 55), new Vector2(280, 70), TakeAll);
        MakeRectButton(_panel.transform, "Close", "CLOSE",
            new Vector2(0.5f, 0f), new Vector2(150, 55), new Vector2(280, 70), Close);

        _root.SetActive(false);
    }

    public void Open(Chest chest)
    {
        if (chest == null || _root == null) return;
        _current = chest;
        Refresh();
        _root.SetActive(true);

        if (_player == null) _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (_root != null) _root.SetActive(false);
        _current = null;

        if (_player != null) _player.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    void TakeAll()
    {
        if (_current == null) return;
        _current.TakeAll();
        Refresh();
    }

    void Refresh()
    {
        if (_itemsContent == null) return;

        for (int i = _itemsContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(_itemsContent.GetChild(i).gameObject);

        if (_current == null) return;

        if (_current.items == null || _current.items.Count == 0)
        {
            var emptyGO = NewGO("Empty", _itemsContent);
            var t = emptyGO.AddComponent<TextMeshProUGUI>();
            t.text = "(empty)";
            t.fontSize = 36;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.6f, 0.55f, 0.5f);
            t.fontStyle = FontStyles.Italic;
            var le = emptyGO.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            return;
        }

        for (int i = 0; i < _current.items.Count; i++)
        {
            int idx = i;
            var it = _current.items[i];
            string label = it.count > 1 ? $"TAKE  {it.name}  x{it.count}" : $"TAKE  {it.name}";
            var btn = MakeListButton(_itemsContent, "Item_" + i, label, () =>
            {
                if (_current == null) return;
                _current.TakeItem(idx);
                Refresh();
            });
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 72;
            le.minHeight = 72;
        }
    }

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, int size, TextAlignmentOptions align)
    {
        var go = NewGO(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = ColText;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    static Button MakeRectButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var go = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = ColBtn;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var btn = go.AddComponent<Button>();
        StyleButton(btn, img);

        var txt = MakeText(go.transform, "Label", label, 30, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform);

        btn.onClick.AddListener(onClick);
        return btn;
    }

    static Button MakeListButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = ColBtn;

        var btn = go.AddComponent<Button>();
        StyleButton(btn, img);

        var txt = MakeText(go.transform, "Label", label, 30, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform);

        btn.onClick.AddListener(onClick);
        return btn;
    }

    static void StyleButton(Button btn, Image img)
    {
        var colors = btn.colors;
        colors.normalColor = ColBtn;
        colors.highlightedColor = ColBtnHi;
        colors.pressedColor = ColBtnDn;
        colors.selectedColor = ColBtnHi;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.targetGraphic = img;

        var outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(1, -1);
    }
}
