using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.I;

    static readonly Color ColBg = new Color(0.10f, 0.08f, 0.06f, 0.97f);
    static readonly Color ColHeader = new Color(0.18f, 0.13f, 0.08f, 1f);
    static readonly Color ColAccent = new Color(0.85f, 0.65f, 0.30f, 1f);
    static readonly Color ColSlot = new Color(0.16f, 0.12f, 0.08f, 1f);
    static readonly Color ColSlotHi = new Color(0.30f, 0.22f, 0.14f, 1f);
    static readonly Color ColText = new Color(0.95f, 0.90f, 0.78f, 1f);

    GameObject _root;
    Transform _itemsGrid;
    PlayerController _player;
    PlayerInventory _inv;
    InventoryItem _selectedItem;

    void Awake()
    {
        BuildUI();
        PlayerInventory.OnInventoryChanged += Refresh;
        PlayerInventory.OnHotbarSlotChanged += (i, it) => Refresh();
    }

    void OnDestroy()
    {
        PlayerInventory.OnInventoryChanged -= Refresh;
    }

    void Start()
    {
        _inv = PlayerInventory.Instance;
        _player = FindFirstObjectByType<PlayerController>();
        Close();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_root.activeSelf) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (_inv == null) _inv = PlayerInventory.Instance;
        Refresh();
        _root.SetActive(true);
        if (_player != null) _player.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        _root.SetActive(false);
        if (_player != null) _player.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("InventoryUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        _root = NewGO("Root", canvasGO.transform);
        var dim = _root.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.6f);
        Stretch(_root.GetComponent<RectTransform>());

        var panel = NewGO("Panel", _root.transform);
        var bg = panel.AddComponent<Image>();
        bg.color = ColBg;
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(800, 600);
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = ColAccent;
        outline.effectDistance = new Vector2(3, -3);

        var header = NewGO("Header", panel.transform);
        header.AddComponent<Image>().color = ColHeader;
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1);
        hrt.anchorMax = new Vector2(1, 1);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0, 70);
        hrt.anchoredPosition = Vector2.zero;

        var title = MakeText(header.transform, "Title", "INVENTORY", 40, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = ColAccent;
        Stretch(title.rectTransform);

        var hint = MakeText(panel.transform, "Hint",
            "Click an item, then press 1 or 2 to assign to hotbar.   [I] to close",
            18, TextAlignmentOptions.Center);
        hint.color = new Color(0.7f, 0.65f, 0.55f);
        var hrt2 = hint.rectTransform;
        hrt2.anchorMin = new Vector2(0, 0);
        hrt2.anchorMax = new Vector2(1, 0);
        hrt2.pivot = new Vector2(0.5f, 0);
        hrt2.sizeDelta = new Vector2(0, 30);
        hrt2.anchoredPosition = new Vector2(0, 10);

        var grid = NewGO("Grid", panel.transform);
        var grt = grid.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0, 0);
        grt.anchorMax = new Vector2(1, 1);
        grt.offsetMin = new Vector2(30, 50);
        grt.offsetMax = new Vector2(-30, -90);
        var gl = grid.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(110, 110);
        gl.spacing = new Vector2(10, 10);
        gl.padding = new RectOffset(10, 10, 10, 10);
        _itemsGrid = grid.transform;
    }

    void Refresh()
    {
        if (_itemsGrid == null) return;
        for (int i = _itemsGrid.childCount - 1; i >= 0; i--)
            DestroyImmediate(_itemsGrid.GetChild(i).gameObject);

        if (_inv == null) return;

        int total = _inv.maxSlots;
        for (int i = 0; i < total; i++)
        {
            InventoryItem it = (i < _inv.items.Count) ? _inv.items[i] : null;
            var slot = NewGO("Slot_" + i, _itemsGrid);
            var img = slot.AddComponent<Image>();
            img.color = (it != null && it == _selectedItem) ? ColSlotHi : ColSlot;

            var outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            if (it != null)
            {
                var captured = it;
                var btn = slot.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = (it == _selectedItem) ? ColSlotHi : ColSlot;
                colors.highlightedColor = ColSlotHi;
                btn.colors = colors;
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => { _selectedItem = captured; Refresh(); });

                for (int h = 0; h < _inv.hotbarSize; h++)
                {
                    if (_inv.hotbar[h] == it)
                    {
                        var bind = MakeText(slot.transform, "Bind", (h + 1).ToString(), 22, TextAlignmentOptions.TopRight);
                        bind.color = ColAccent;
                        bind.fontStyle = FontStyles.Bold;
                        var brt = bind.rectTransform;
                        brt.anchorMin = new Vector2(1, 1);
                        brt.anchorMax = new Vector2(1, 1);
                        brt.pivot = new Vector2(1, 1);
                        brt.anchoredPosition = new Vector2(-5, -3);
                        brt.sizeDelta = new Vector2(25, 25);
                        bind.raycastTarget = false;
                    }
                }

                if (it.icon != null)
                {
                    var iconGO = NewGO("Icon", slot.transform);
                    var icon = iconGO.AddComponent<Image>();
                    icon.sprite = it.icon;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    var irt = icon.rectTransform;
                    irt.anchorMin = Vector2.zero;
                    irt.anchorMax = Vector2.one;
                    irt.offsetMin = new Vector2(10, 25);
                    irt.offsetMax = new Vector2(-10, -10);
                }

                var name = MakeText(slot.transform, "Name", it.displayName, 14, TextAlignmentOptions.Bottom);
                var nrt = name.rectTransform;
                nrt.anchorMin = new Vector2(0, 0);
                nrt.anchorMax = new Vector2(1, 0);
                nrt.pivot = new Vector2(0.5f, 0);
                nrt.anchoredPosition = new Vector2(0, 5);
                nrt.sizeDelta = new Vector2(0, 18);
                name.raycastTarget = false;
            }
        }

        if (_selectedItem != null && _root.activeSelf)
        {
            for (int h = 0; h < _inv.hotbarSize; h++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + h))
                {
                    _inv.AssignToHotbar(_selectedItem, h);
                    Refresh();
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!_root.activeSelf || _selectedItem == null || _inv == null) return;
        for (int h = 0; h < _inv.hotbarSize; h++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + h))
            {
                _inv.AssignToHotbar(_selectedItem, h);
                Refresh();
            }
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
        return t;
    }
}
