using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
	[Header("Layout")]
	public Vector2 slotSize = new Vector2(70f, 70f);

	public float spacing = 8f;

	public Vector2 anchorOffset = new Vector2(0f, 80f);

	private static readonly Color ColSlot = new Color(0.1f, 0.045f, 0.03f, 0.92f);

	private static readonly Color ColSlotActive = new Color(0.2f, 0.09f, 0.05f, 0.96f);

	private static readonly Color ColEdgeDim = new Color(UIKit.Edge.r, UIKit.Edge.g, UIKit.Edge.b, 0.5f);

	private static readonly Color ColText = UIKit.TextCol;

	private Image[] _slotBg;

	private Image[] _slotIcon;

	private Image[] _slotOverlay;

	private Outline[] _slotOutline;

	private TextMeshProUGUI[] _slotCount;

	private TextMeshProUGUI _nameLabel;

	private int _activeIdx = -1;

	private void Awake()
	{
		BuildUI();
		PlayerInventory.OnHotbarSlotChanged += OnHotbarSlot;
		PlayerInventory.OnActiveSlotChanged += OnActiveSlot;
		PlayerInventory.OnInventoryChanged += RefreshCounts;
	}

	private void OnDestroy()
	{
		PlayerInventory.OnHotbarSlotChanged -= OnHotbarSlot;
		PlayerInventory.OnActiveSlotChanged -= OnActiveSlot;
		PlayerInventory.OnInventoryChanged -= RefreshCounts;
	}

	private void Start()
	{
		if (!(PlayerInventory.Instance == null))
		{
			for (int i = 0; i < PlayerInventory.Instance.hotbarSize; i++)
			{
				OnHotbarSlot(i, PlayerInventory.Instance.hotbar[i]);
			}
			OnActiveSlot(PlayerInventory.Instance.activeSlot, null);
		}
	}

	private void BuildUI()
	{
		GameObject gameObject = new GameObject("HotbarUI_Canvas");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 50;
		CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		gameObject.AddComponent<GraphicRaycaster>();
		int num = ((PlayerInventory.Instance != null) ? PlayerInventory.Instance.hotbarSize : 2);
		_slotBg = new Image[num];
		_slotIcon = new Image[num];
		_slotOverlay = new Image[num];
		_slotOutline = new Outline[num];
		_slotCount = new TextMeshProUGUI[num];
		GameObject gameObject2 = NewGO("Container", gameObject.transform);
		RectTransform component = gameObject2.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0f);
		component.anchorMax = new Vector2(0.5f, 0f);
		component.pivot = new Vector2(0.5f, 0f);
		component.anchoredPosition = anchorOffset;
		float x = (float)num * slotSize.x + (float)(num - 1) * spacing;
		component.sizeDelta = new Vector2(x, slotSize.y);
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject3 = NewGO("Slot_" + i, gameObject2.transform);
			RectTransform component2 = gameObject3.GetComponent<RectTransform>();
			Vector2 anchorMin = (component2.anchorMax = new Vector2(0f, 0.5f));
			component2.anchorMin = anchorMin;
			component2.pivot = new Vector2(0f, 0.5f);
			component2.sizeDelta = slotSize;
			component2.anchoredPosition = new Vector2((float)i * (slotSize.x + spacing), 0f);
			Image image = gameObject3.AddComponent<Image>();
			image.color = ColSlot;
			_slotBg[i] = image;
			Outline outline = gameObject3.AddComponent<Outline>();
			outline.effectColor = ColEdgeDim;
			outline.effectDistance = new Vector2(2f, -2f);
			_slotOutline[i] = outline;
			TextMeshProUGUI textMeshProUGUI = NewGO("Num", gameObject3.transform).AddComponent<TextMeshProUGUI>();
			textMeshProUGUI.text = (i + 1).ToString();
			textMeshProUGUI.fontSize = 18f;
			textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
			textMeshProUGUI.color = ColText;
			textMeshProUGUI.fontStyle = FontStyles.Bold;
			RectTransform rectTransform = textMeshProUGUI.rectTransform;
			rectTransform.anchorMin = new Vector2(0f, 1f);
			rectTransform.anchorMax = new Vector2(0f, 1f);
			rectTransform.pivot = new Vector2(0f, 1f);
			rectTransform.anchoredPosition = new Vector2(5f, -3f);
			rectTransform.sizeDelta = new Vector2(20f, 20f);
			Image image2 = NewGO("Icon", gameObject3.transform).AddComponent<Image>();
			image2.preserveAspect = true;
			image2.enabled = false;
			RectTransform rectTransform2 = image2.rectTransform;
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.offsetMin = new Vector2(8f, 8f);
			rectTransform2.offsetMax = new Vector2(-8f, -8f);
			_slotIcon[i] = image2;
			Image image3 = NewGO("IconOverlay", gameObject3.transform).AddComponent<Image>();
			image3.preserveAspect = true;
			image3.raycastTarget = false;
			image3.enabled = false;
			RectTransform rectTransform3 = image3.rectTransform;
			rectTransform3.anchorMin = Vector2.zero;
			rectTransform3.anchorMax = Vector2.one;
			rectTransform3.offsetMin = new Vector2(8f, 8f);
			rectTransform3.offsetMax = new Vector2(-8f, -8f);
			_slotOverlay[i] = image3;
			TextMeshProUGUI textMeshProUGUI2 = NewGO("Count", gameObject3.transform).AddComponent<TextMeshProUGUI>();
			textMeshProUGUI2.fontSize = 17f;
			textMeshProUGUI2.alignment = TextAlignmentOptions.BottomRight;
			textMeshProUGUI2.color = Color.white;
			textMeshProUGUI2.fontStyle = FontStyles.Bold;
			textMeshProUGUI2.raycastTarget = false;
			textMeshProUGUI2.text = "";
			Outline outline2 = textMeshProUGUI2.gameObject.AddComponent<Outline>();
			outline2.effectColor = new Color(0f, 0f, 0f, 0.85f);
			outline2.effectDistance = new Vector2(1.5f, -1.5f);
			RectTransform rectTransform4 = textMeshProUGUI2.rectTransform;
			rectTransform4.anchorMin = new Vector2(1f, 0f);
			rectTransform4.anchorMax = new Vector2(1f, 0f);
			rectTransform4.pivot = new Vector2(1f, 0f);
			rectTransform4.anchoredPosition = new Vector2(-4f, 3f);
			rectTransform4.sizeDelta = new Vector2(40f, 20f);
			_slotCount[i] = textMeshProUGUI2;
		}
		GameObject gameObject4 = NewGO("ActiveName", gameObject2.transform);
		_nameLabel = gameObject4.AddComponent<TextMeshProUGUI>();
		_nameLabel.fontSize = 20f;
		_nameLabel.alignment = TextAlignmentOptions.Center;
		_nameLabel.color = ColText;
		_nameLabel.fontStyle = FontStyles.Bold;
		_nameLabel.text = "";
		_nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
		_nameLabel.raycastTarget = false;
		RectTransform rectTransform5 = _nameLabel.rectTransform;
		rectTransform5.anchorMin = new Vector2(0.5f, 1f);
		rectTransform5.anchorMax = new Vector2(0.5f, 1f);
		rectTransform5.pivot = new Vector2(0.5f, 0f);
		rectTransform5.anchoredPosition = new Vector2(0f, 8f);
		rectTransform5.sizeDelta = new Vector2(360f, 26f);
	}

	private void OnHotbarSlot(int idx, InventoryItem item)
	{
		if (idx < 0 || idx >= _slotIcon.Length)
		{
			return;
		}
		if (item != null && !item.IsEmpty && item.icon != null)
		{
			_slotIcon[idx].sprite = item.icon;
			_slotIcon[idx].color = ((item.iconTint.a <= 0f) ? Color.white : item.iconTint);
			_slotIcon[idx].enabled = true;
			if (item.iconOverlay != null)
			{
				_slotOverlay[idx].sprite = item.iconOverlay;
				_slotOverlay[idx].enabled = true;
			}
			else
			{
				_slotOverlay[idx].enabled = false;
			}
		}
		else
		{
			_slotIcon[idx].enabled = false;
			_slotOverlay[idx].enabled = false;
		}
		SetCount(idx, item);
		if (idx == _activeIdx)
		{
			UpdateNameLabel();
		}
	}

	private void SetCount(int idx, InventoryItem item)
	{
		if (_slotCount != null && idx >= 0 && idx < _slotCount.Length)
		{
			bool flag = item != null && !item.IsEmpty && item.count > 1;
			_slotCount[idx].text = (flag ? item.count.ToString() : "");
		}
	}

	private void RefreshCounts()
	{
		PlayerInventory instance = PlayerInventory.Instance;
		if (!(instance == null) && _slotCount != null)
		{
			for (int i = 0; i < _slotCount.Length && i < instance.hotbarSize; i++)
			{
				SetCount(i, instance.hotbar[i]);
			}
		}
	}

	private void OnActiveSlot(int idx, InventoryItem item)
	{
		_activeIdx = idx;
		for (int i = 0; i < _slotBg.Length; i++)
		{
			bool flag = i == idx;
			_slotBg[i].color = (flag ? ColSlotActive : ColSlot);
			_slotOutline[i].effectColor = (flag ? UIKit.Edge : ColEdgeDim);
			_slotOutline[i].effectDistance = (flag ? new Vector2(3f, -3f) : new Vector2(2f, -2f));
		}
		UpdateNameLabel();
	}

	private void UpdateNameLabel()
	{
		if (!(_nameLabel == null))
		{
			PlayerInventory instance = PlayerInventory.Instance;
			InventoryItem inventoryItem = ((instance != null && _activeIdx >= 0 && _activeIdx < instance.hotbarSize) ? instance.hotbar[_activeIdx] : null);
			_nameLabel.text = ((inventoryItem != null && !inventoryItem.IsEmpty) ? inventoryItem.displayName : "");
		}
	}

	private static GameObject NewGO(string name, Transform parent)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		return obj;
	}
}
