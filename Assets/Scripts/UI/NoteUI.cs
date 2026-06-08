using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class NoteUI : MonoBehaviour
{
    static NoteUI _instance;
    GameObject _root;
    TextMeshProUGUI _title;
    TextMeshProUGUI _body;
    Image _image;
    bool _open;

    public static bool IsOpen => _instance != null && _instance._open;

    static NoteUI Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("NoteUI");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<NoteUI>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static void Show(string title, string body, Sprite sketch) => Instance.Open(title, body, sketch);

    void Build()
    {
        var canvasGO = new GameObject("NoteUI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root = UIKit.NewRect("Root", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UIKit.Stretch((RectTransform)_root.transform);
        _root.AddComponent<Image>().color = UIKit.Dim;

        var box = UIKit.Box("NotePanel", _root.transform, new Vector2(640f, 640f));

        _title = UIKit.Text(box.transform, "NOTE", 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        var trt = _title.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -24f);
        trt.sizeDelta = new Vector2(560f, 50f);
        _title.color = UIKit.Edge;
        _title.characterSpacing = 8f;

        var imgGO = UIKit.NewRect("Sketch", box.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -240f), new Vector2(320f, 320f));
        _image = imgGO.AddComponent<Image>();
        _image.preserveAspect = true;
        _image.raycastTarget = false;
        _image.enabled = false;

        _body = UIKit.Text(box.transform, "", 26f, FontStyles.Normal, TextAlignmentOptions.Top);
        var brt = _body.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = new Vector2(40f, 96f);
        brt.offsetMax = new Vector2(-40f, -90f);
        _body.textWrappingMode = TextWrappingModes.Normal;

        UIKit.Button(box.transform, 0f, -286f, 220f, 54f, "Close", Close);

        _root.SetActive(false);
    }

    void Open(string title, string body, Sprite sketch)
    {
        if (_root == null) Build();
        _title.text = string.IsNullOrEmpty(title) ? "NOTE" : title.ToUpperInvariant();
        _body.text = string.IsNullOrEmpty(body) ? "(the writing is faded and unreadable)" : body;

        bool hasSketch = sketch != null;
        _image.enabled = hasSketch;
        if (hasSketch) _image.sprite = sketch;
        _body.rectTransform.offsetMax = new Vector2(-40f, hasSketch ? -420f : -90f);

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        _open = true;
    }

    void Close()
    {
        if (_root != null) _root.SetActive(false);
        _open = false;
    }

    void Update()
    {
        if (!_open) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) Close();
    }
}
