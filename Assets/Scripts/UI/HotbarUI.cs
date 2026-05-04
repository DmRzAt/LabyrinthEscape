using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarUI : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 slotSize = new Vector2(70f, 70f);
    public float spacing = 8f;
    public Vector2 anchorOffset = new Vector2(0f, 80f);

    static readonly Color ColSlot = new Color(0.10f, 0.08f, 0.06f, 0.85f);
    static readonly Color ColSlotActive = new Color(0.85f, 0.65f, 0.30f, 1f);
    static readonly Color ColText = new Color(0.95f, 0.90f, 0.78f, 1f);

    Image[] _slotBg;
    Image[] _slotIcon;
    TextMeshProUGUI[] _slotLabel;
    Outline[] _slotOutline;

    void Awake()
    {
        BuildUI();
        PlayerInventory.OnHotbarSlotChanged += OnHotbarSlot;
        PlayerInventory.OnActiveSlotChanged += OnActiveSlot;
    }

    void OnDestroy()
    {
        PlayerInventory.OnHotbarSlotChanged -= OnHotbarSlot;
        PlayerInventory.OnActiveSlotChanged -= OnActiveSlot;
    }

    void Start()
    {
        if (PlayerInventory.Instance == null) return;
        for (int i = 0; i < PlayerInventory.Instance.hotbarSize; i++)
            OnHotbarSlot(i, PlayerInventory.Instance.hotbar[i]);
        OnActiveSlot(PlayerInventory.Instance.activeSlot, null);
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HotbarUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        int n = PlayerInventory.Instance != null ? PlayerInventory.Instance.hotbarSize : 2;
        _slotBg = new Image[n];
        _slotIcon = new Image[n];
        _slotLabel = new TextMeshProUGUI[n];
        _slotOutline = new Outline[n];

        var container = NewGO("Container", canvasGO.transform);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchorOffset;
        float totalWidth = n * slotSize.x + (n - 1) * spacing;
        rt.sizeDelta = new Vector2(totalWidth, slotSize.y);

        for (int i = 0; i < n; i++)
        {
            var slotGO = NewGO("Slot_" + i, container.transform);
            var srt = slotGO.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0f, 0.5f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.sizeDelta = slotSize;
            srt.anchoredPosition = new Vector2(i * (slotSize.x + spacing), 0f);

            var bg = slotGO.AddComponent<Image>();
            bg.color = ColSlot;
            _slotBg[i] = bg;

            var outline = slotGO.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.7f);
            outline.effectDistance = new Vector2(2, -2);
            _slotOutline[i] = outline;

            var numGO = NewGO("Num", slotGO.transform);
            var num = numGO.AddComponent<TextMeshProUGUI>();
            num.text = (i + 1).ToString();
            num.fontSize = 18;
            num.alignment = TextAlignmentOptions.TopLeft;
            num.color = ColText;
            num.fontStyle = FontStyles.Bold;
            var nrt = num.rectTransform;
            nrt.anchorMin = new Vector2(0, 1);
            nrt.anchorMax = new Vector2(0, 1);
            nrt.pivot = new Vector2(0, 1);
            nrt.anchoredPosition = new Vector2(5, -3);
            nrt.sizeDelta = new Vector2(20, 20);

            var iconGO = NewGO("Icon", slotGO.transform);
            var icon = iconGO.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
            var irt = icon.rectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(8, 8);
            irt.offsetMax = new Vector2(-8, -8);
            _slotIcon[i] = icon;

            var labelGO = NewGO("Label", slotGO.transform);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Center;
            label.color = ColText;
            label.fontStyle = FontStyles.Bold;
            label.text = "";
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(2, 2);
            lrt.offsetMax = new Vector2(-2, -2);
            _slotLabel[i] = label;
        }
    }

    void OnHotbarSlot(int idx, InventoryItem item)
    {
        if (idx < 0 || idx >= _slotIcon.Length) return;
        if (item != null && !item.IsEmpty && item.icon != null)
        {
            _slotIcon[idx].sprite = item.icon;
            _slotIcon[idx].enabled = true;
        }
        else _slotIcon[idx].enabled = false;

        _slotLabel[idx].text = (item != null && !item.IsEmpty) ? item.displayName : "";
    }

    void OnActiveSlot(int idx, InventoryItem item)
    {
        for (int i = 0; i < _slotBg.Length; i++)
        {
            bool active = i == idx;
            _slotBg[i].color = active ? new Color(0.18f, 0.13f, 0.08f, 0.95f) : ColSlot;
            _slotOutline[i].effectColor = active ? ColSlotActive : new Color(0, 0, 0, 0.7f);
            _slotOutline[i].effectDistance = active ? new Vector2(3, -3) : new Vector2(2, -2);
        }
    }

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
