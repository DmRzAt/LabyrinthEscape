using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using TMPro;

public class MazeMap : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    static MazeMap _instance;

    public static void RegisterChestMarker(Chest chest)
    {
        if (_instance != null && chest != null) _instance.AddMarker(chest, MarkerKind.Chest);
    }

    [Header("Toggle")]
    public Key toggleKey = Key.Tab;

    [Header("Bounds")]
    public string labyrinthRootName = "Labyrinth";
    public float boundsMargin = 4f;

    [Header("Map / fog")]
    [Tooltip("Texture resolution is auto-scaled from the labyrinth size, clamped to this max.")]
    public int resolution = 384;
    [Tooltip("World-space radius revealed around the player.")]
    public float revealRadius = 8f;
    public float repaintMoveThreshold = 0.5f;
    [Tooltip("Horizontal tolerance when sampling NavMesh; fattens corridors toward real width.")]
    public float pathSampleDistance = 0.1f;

    [Header("Look")]
    public float mapPixelSize = 860f;
    public Color pathColor = new Color(0.80f, 0.71f, 0.52f, 1f);
    public Color wallColor = new Color(0.15f, 0.12f, 0.10f, 1f);
    public Color fogColor  = new Color(0.05f, 0.045f, 0.04f, 1f);
    public Color accent    = new Color(0.90f, 0.45f, 0.12f, 1f);
    public Color frameColor = new Color(0.13f, 0.10f, 0.08f, 1f);

    enum MarkerKind { Key, Door, Chest }

    class Marker
    {
        public MonoBehaviour src;
        public Transform t;
        public RectTransform rt;
    }

    Texture2D _mapTex;
    Texture2D _fogTex;
    Color32[] _fog;
    bool[] _isPath;
    GameObject _canvasRoot;
    RectTransform _container;
    RectTransform _marker;
    Transform _player;
    Sprite _dotSprite;
    Texture2D _arrowTex, _dotTex;
    readonly List<Marker> _markers = new List<Marker>();

    readonly Queue<int> _revealQueue = new Queue<int>();
    int[] _stamp;
    int _stampId;

    int _dx0, _dy0, _dx1, _dy1;
    bool _fogDirty;
    Color32[] _fogSub;

    Vector3 _center;
    float _half;
    bool _open;

    Vector3 _lastPaintPos;

    void Start()
    {
        _instance = this;
        ComputeBounds();
        resolution = Mathf.Clamp(Mathf.CeilToInt(2f * _half * 1.5f), 180, resolution);
        AllocTextures();
        BuildFog();
        BuildUI();
        BuildMarkers();
        SetOpen(false);
        _lastPaintPos = new Vector3(99999f, 0f, 99999f);
        StartCoroutine(FillBaseMap());
    }

    void ComputeBounds()
    {
        var root = GameObject.Find(labyrinthRootName);
        Renderer[] rends = root != null
            ? root.GetComponentsInChildren<Renderer>()
            : Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        bool has = false;
        Bounds b = new Bounds();
        foreach (var r in rends)
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.name.IndexOf("wall", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!has)
        {
            foreach (var r in rends)
            {
                if (r is ParticleSystemRenderer) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        if (!has) { _center = Vector3.zero; _half = 50f; return; }
        _center = new Vector3(b.center.x, b.center.y, b.center.z);
        _half = Mathf.Max(b.extents.x, b.extents.z) + boundsMargin;
    }

    void AllocTextures()
    {
        int N = resolution;
        _mapTex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        _isPath = new bool[N * N];
        _stamp = new int[N * N];

        var fill = new Color32[N * N];
        Color32 wall = wallColor;
        for (int i = 0; i < fill.Length; i++) fill[i] = wall;
        _mapTex.SetPixels32(fill);
        _mapTex.Apply();
    }

    IEnumerator FillBaseMap()
    {
        int N = resolution;
        var px = new Color32[N * N];
        Color32 path = pathColor, wall = wallColor, edge = Color32.Lerp(wallColor, Color.black, 0.4f);
        float span = 2f * _half;

        float navY = _center.y;
        if (NavMesh.SamplePosition(new Vector3(_center.x, _center.y, _center.z), out var calib, 300f, NavMesh.AllAreas))
            navY = calib.position.y;

        for (int y = 0; y < N; y++)
        {
            float wz = _center.z - _half + (y + 0.5f) / N * span;
            for (int x = 0; x < N; x++)
            {
                float wx = _center.x - _half + (x + 0.5f) / N * span;
                bool p = NavMesh.SamplePosition(new Vector3(wx, navY, wz), out _, pathSampleDistance, NavMesh.AllAreas);
                _isPath[y * N + x] = p;
                px[y * N + x] = p ? path : wall;
            }
            if ((y & 7) == 0) yield return null;
        }

        for (int y = 1; y < N - 1; y++)
        for (int x = 1; x < N - 1; x++)
        {
            int i = y * N + x;
            if (_isPath[i]) continue;
            if (_isPath[i - 1] || _isPath[i + 1] || _isPath[i - N] || _isPath[i + N])
                px[i] = edge;
        }

        _mapTex.SetPixels32(px);
        _mapTex.Apply();
    }

    void BuildFog()
    {
        int N = resolution;
        _fogTex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        _fog = new Color32[N * N];
        Color32 opaque = fogColor; opaque.a = 255;
        for (int i = 0; i < _fog.Length; i++) _fog[i] = opaque;
        _fogTex.SetPixels32(_fog);
        _fogTex.Apply();
        ResetDirty();
    }

    void BuildUI()
    {
        _canvasRoot = new GameObject("MazeMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = _canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = _canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = NewImage("Dim", _canvasRoot.transform, new Color(0f, 0f, 0f, 0.8f));
        Stretch(dim.rectTransform);

        var frame = NewImage("Frame", _canvasRoot.transform, frameColor);
        frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        frame.rectTransform.sizeDelta = new Vector2(mapPixelSize + 40f, mapPixelSize + 40f);
        frame.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        var frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.6f); frameShadow.effectDistance = new Vector2(6f, -6f);

        var gold = NewImage("AccentBorder", frame.transform, accent);
        gold.rectTransform.anchorMin = gold.rectTransform.anchorMax = gold.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gold.rectTransform.sizeDelta = new Vector2(mapPixelSize + 14f, mapPixelSize + 14f);

        _container = NewRect("Container", frame.transform);
        _container.anchorMin = _container.anchorMax = _container.pivot = new Vector2(0.5f, 0.5f);
        _container.sizeDelta = new Vector2(mapPixelSize, mapPixelSize);

        var mapGo = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
        mapGo.transform.SetParent(_container, false);
        mapGo.GetComponent<RawImage>().texture = _mapTex;
        Stretch(mapGo.GetComponent<RectTransform>());

        var fogGo = new GameObject("Fog", typeof(RectTransform), typeof(RawImage));
        fogGo.transform.SetParent(_container, false);
        var fogImg = fogGo.GetComponent<RawImage>();
        fogImg.texture = _fogTex; fogImg.raycastTarget = false;
        Stretch(fogGo.GetComponent<RectTransform>());

        AddCorner(frame.transform, new Vector2(0f, 0f));
        AddCorner(frame.transform, new Vector2(1f, 0f));
        AddCorner(frame.transform, new Vector2(0f, 1f));
        AddCorner(frame.transform, new Vector2(1f, 1f));

        var mGo = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        mGo.transform.SetParent(_container, false);
        _marker = mGo.GetComponent<RectTransform>();
        _marker.sizeDelta = new Vector2(28f, 28f);
        var mImg = mGo.GetComponent<Image>();
        mImg.sprite = MakeArrowSprite();
        mImg.color = new Color(0.95f, 0.25f, 0.15f, 1f);
        mImg.raycastTarget = false;
        var mOutline = mGo.AddComponent<Outline>();
        mOutline.effectColor = new Color(0f, 0f, 0f, 0.85f); mOutline.effectDistance = new Vector2(2f, -2f);

        var n = NewText("North", _container, "N", 30f, FontStyles.Bold);
        n.color = accent;
        n.rectTransform.anchorMin = n.rectTransform.anchorMax = n.rectTransform.pivot = new Vector2(0.5f, 1f);
        n.rectTransform.sizeDelta = new Vector2(40f, 40f);
        n.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        n.alignment = TextAlignmentOptions.Center;

        var title = NewText("Title", _canvasRoot.transform, "MAP", 52f, FontStyles.Bold);
        title.color = accent; title.characterSpacing = 12f;
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        title.rectTransform.sizeDelta = new Vector2(600f, 64f);
        title.rectTransform.anchoredPosition = new Vector2(0f, mapPixelSize * 0.5f + 46f);
        title.alignment = TextAlignmentOptions.Center;

        var hint = NewText("Hint", _canvasRoot.transform, "Tab — close", 24f, FontStyles.Italic);
        hint.color = new Color(0.78f, 0.76f, 0.7f, 0.8f);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        hint.rectTransform.sizeDelta = new Vector2(600f, 36f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -mapPixelSize * 0.5f - 42f);
        hint.alignment = TextAlignmentOptions.Center;
    }

    void BuildMarkers()
    {
        foreach (var k in Object.FindObjectsByType<KeyPickup>(FindObjectsSortMode.None))
            AddMarker(k, MarkerKind.Key);
        foreach (var d in Object.FindObjectsByType<LockedDoor>(FindObjectsSortMode.None))
            AddMarker(d, MarkerKind.Door);
        foreach (var c in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            AddMarker(c, MarkerKind.Chest);
    }

    void AddMarker(MonoBehaviour src, MarkerKind kind)
    {
        if (src == null) return;
        Color col = kind switch
        {
            MarkerKind.Key   => accent,
            MarkerKind.Door  => new Color(0.30f, 0.80f, 0.35f, 1f),
            _                => new Color(0.85f, 0.70f, 0.45f, 1f),
        };
        var go = new GameObject("Marker_" + kind, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_container, false);
        var img = go.GetComponent<Image>();
        img.sprite = DotSprite();
        img.color = col;
        img.raycastTarget = false;
        var o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f); o.effectDistance = new Vector2(1.5f, -1.5f);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(kind == MarkerKind.Key ? 16f : 20f, kind == MarkerKind.Key ? 16f : 20f);
        go.SetActive(false);
        _markers.Add(new Marker { src = src, t = src.transform, rt = rt });
    }

    void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindWithTag(Tags.Player);
            if (p != null) _player = p.transform;
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[toggleKey].wasPressedThisFrame)
            {
                if (_open) SetOpen(false);
                else if (CanOpen()) SetOpen(true);
            }
            else if (_open && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
        }

        if (_player != null &&
            (_player.position - _lastPaintPos).sqrMagnitude >= repaintMoveThreshold * repaintMoveThreshold)
        {
            Reveal(_player.position);
            _lastPaintPos = _player.position;
        }

        if (_fogDirty) FlushFog();

        if (_open && _player != null) UpdateMarkers();
    }

    void Reveal(Vector3 worldPos)
    {
        int N = resolution;
        float span = 2f * _half;
        int cx = Mathf.RoundToInt((worldPos.x - (_center.x - _half)) / span * N);
        int cy = Mathf.RoundToInt((worldPos.z - (_center.z - _half)) / span * N);
        if (cx < 0 || cy < 0 || cx >= N || cy >= N) return;

        int rad = Mathf.Max(1, Mathf.CeilToInt(revealRadius / span * N));
        float core = rad * 0.6f;

        int seed = FindNearestPath(cx, cy, 3);
        if (seed < 0) { RevealCircle(cx, cy, rad, core); return; }

        _stampId++;
        _revealQueue.Clear();
        _revealQueue.Enqueue(seed);
        _stamp[seed] = _stampId;

        while (_revealQueue.Count > 0)
        {
            int idx = _revealQueue.Dequeue();
            int x = idx % N, y = idx / N;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d > rad) continue;

            byte a = (byte)(Mathf.Clamp01((d - core) / Mathf.Max(0.001f, rad - core)) * 255f);
            LowerFog(idx, a);
            if (x > 0)     LowerFog(idx - 1, a);
            if (x < N - 1) LowerFog(idx + 1, a);
            if (y > 0)     LowerFog(idx - N, a);
            if (y < N - 1) LowerFog(idx + N, a);

            Enqueue(x - 1, y, N);
            Enqueue(x + 1, y, N);
            Enqueue(x, y - 1, N);
            Enqueue(x, y + 1, N);
        }
    }

    void Enqueue(int x, int y, int N)
    {
        if (x < 0 || y < 0 || x >= N || y >= N) return;
        int idx = y * N + x;
        if (!_isPath[idx] || _stamp[idx] == _stampId) return;
        _stamp[idx] = _stampId;
        _revealQueue.Enqueue(idx);
    }

    int FindNearestPath(int cx, int cy, int r)
    {
        int N = resolution;
        for (int ring = 0; ring <= r; ring++)
        for (int dy = -ring; dy <= ring; dy++)
        for (int dx = -ring; dx <= ring; dx++)
        {
            int x = cx + dx, y = cy + dy;
            if (x < 0 || y < 0 || x >= N || y >= N) continue;
            if (_isPath[y * N + x]) return y * N + x;
        }
        return -1;
    }

    void RevealCircle(int cx, int cy, int rad, float core)
    {
        int N = resolution;
        int x0 = Mathf.Max(0, cx - rad), x1 = Mathf.Min(N - 1, cx + rad);
        int y0 = Mathf.Max(0, cy - rad), y1 = Mathf.Min(N - 1, cy + rad);
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d > rad) continue;
            byte a = (byte)(Mathf.Clamp01((d - core) / Mathf.Max(0.001f, rad - core)) * 255f);
            LowerFog(y * N + x, a);
        }
    }

    void LowerFog(int idx, byte a)
    {
        if (a >= _fog[idx].a) return;
        var c = _fog[idx]; c.a = a; _fog[idx] = c;
        int N = resolution;
        MarkDirty(idx % N, idx / N);
    }

    void MarkDirty(int x, int y)
    {
        if (x < _dx0) _dx0 = x;
        if (y < _dy0) _dy0 = y;
        if (x > _dx1) _dx1 = x;
        if (y > _dy1) _dy1 = y;
        _fogDirty = true;
    }

    void ResetDirty()
    {
        _dx0 = _dy0 = int.MaxValue;
        _dx1 = _dy1 = int.MinValue;
        _fogDirty = false;
    }

    void FlushFog()
    {
        int N = resolution;
        if (_dx1 < _dx0 || _dy1 < _dy0) { _fogDirty = false; return; }
        int w = _dx1 - _dx0 + 1, h = _dy1 - _dy0 + 1;
        if (_fogSub == null || _fogSub.Length < w * h) _fogSub = new Color32[N * N];
        for (int yy = 0; yy < h; yy++)
        for (int xx = 0; xx < w; xx++)
            _fogSub[yy * w + xx] = _fog[(_dy0 + yy) * N + (_dx0 + xx)];
        _fogTex.SetPixels32(_dx0, _dy0, w, h, _fogSub);
        _fogTex.Apply();
        ResetDirty();
    }

    void UpdateMarkers()
    {
        UpdateRectFromWorld(_marker, _player.position);
        _marker.localEulerAngles = new Vector3(0f, 0f, -_player.eulerAngles.y);

        int N = resolution;
        float span = 2f * _half;
        for (int i = 0; i < _markers.Count; i++)
        {
            var m = _markers[i];
            if (m.src == null) { m.rt.gameObject.SetActive(false); continue; }

            int cx = Mathf.Clamp(Mathf.RoundToInt((m.t.position.x - (_center.x - _half)) / span * N), 0, N - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt((m.t.position.z - (_center.z - _half)) / span * N), 0, N - 1);
            bool explored = _fog[cy * N + cx].a < 160;
            m.rt.gameObject.SetActive(explored);
            if (explored) UpdateRectFromWorld(m.rt, m.t.position);
        }
    }

    void UpdateRectFromWorld(RectTransform rt, Vector3 world)
    {
        float span = 2f * _half;
        float u = (world.x - (_center.x - _half)) / span;
        float v = (world.z - (_center.z - _half)) / span;
        float w = _container.rect.width, h = _container.rect.height;
        rt.anchoredPosition = new Vector2((u - 0.5f) * w, (v - 0.5f) * h);
    }

    void SetOpen(bool open)
    {
        _open = open;
        IsOpen = open;
        if (_canvasRoot != null) _canvasRoot.SetActive(open);
        if (open && _player != null) UpdateMarkers();

        if (GameManager.Instance != null) GameManager.Instance.SetPaused(open);
        else Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    bool CanOpen()
    {
        if (InventoryUI.IsOpen || NoteUI.IsOpen) return false;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return false;
        return GameManager.Instance == null || !GameManager.Instance.IsPaused;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = Color.white; t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    void AddCorner(Transform frame, Vector2 anchor)
    {
        var c = NewImage("Corner", frame, accent);
        var rt = c.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.sizeDelta = new Vector2(26f, 26f);
        rt.anchoredPosition = new Vector2(anchor.x < 0.5f ? 4f : -4f, anchor.y < 0.5f ? 4f : -4f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    Sprite MakeArrowSprite()
    {
        const int s = 32;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        for (int y = 0; y < s; y++)
        {
            float ty = (float)y / (s - 1);
            float halfW = (1f - ty) * 0.5f;
            for (int x = 0; x < s; x++)
            {
                float nx = (float)x / (s - 1) - 0.5f;
                if (ty > 0.18f && Mathf.Abs(nx) <= halfW) px[y * s + x] = white;
            }
        }
        t.SetPixels32(px); t.Apply();
        _arrowTex = t;
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite DotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        const int s = 32;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[s * s];
        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);
        float c = (s - 1) * 0.5f, r = c - 1f;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float dx = x - c, dy = y - c;
            px[y * s + x] = (dx * dx + dy * dy <= r * r) ? white : clear;
        }
        t.SetPixels32(px); t.Apply();
        _dotTex = t;
        _dotSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return _dotSprite;
    }

    void OnDestroy()
    {
        if (_mapTex != null) Destroy(_mapTex);
        if (_fogTex != null) Destroy(_fogTex);
        if (_arrowTex != null) Destroy(_arrowTex);
        if (_dotTex != null) Destroy(_dotTex);
        if (_open) IsOpen = false;
        if (_instance == this) _instance = null;
    }
}
