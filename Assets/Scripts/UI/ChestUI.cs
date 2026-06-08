using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChestUI : MonoBehaviour
{
	private class RowView
	{
		public GameObject go;

		private Button _btn;

		private Image _icon;

		private Image _iconOverlay;

		private TextMeshProUGUI _name;

		private TextMeshProUGUI _count;

		private TextMeshProUGUI _take;

		public static RowView Create(Transform parent)
		{
			RowView rowView = new RowView();
			GameObject gameObject = (rowView.go = NewGO("Item", parent));
			Image image = gameObject.AddComponent<Image>();
			image.color = ColRow;
			rowView._btn = gameObject.AddComponent<Button>();
			StyleButton(rowView._btn, image, ColRow, ColRowHi);
			LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
			layoutElement.preferredHeight = 56f;
			layoutElement.minHeight = 56f;
			GameObject gameObject2 = NewGO("Accent", gameObject.transform);
			Image image2 = gameObject2.AddComponent<Image>();
			image2.color = ColAccent;
			image2.raycastTarget = false;
			RectTransform component = gameObject2.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0f, 0f);
			component.anchorMax = new Vector2(0f, 1f);
			component.pivot = new Vector2(0f, 0.5f);
			component.sizeDelta = new Vector2(4f, 0f);
			component.anchoredPosition = Vector2.zero;
			GameObject gameObject3 = NewGO("IconSlot", gameObject.transform);
			Image image3 = gameObject3.AddComponent<Image>();
			image3.color = new Color(0.055f, 0.04f, 0.025f, 1f);
			image3.raycastTarget = false;
			RectTransform component2 = gameObject3.GetComponent<RectTransform>();
			Vector2 anchorMin = (component2.anchorMax = new Vector2(0f, 0.5f));
			component2.anchorMin = anchorMin;
			component2.pivot = new Vector2(0f, 0.5f);
			component2.sizeDelta = new Vector2(44f, 44f);
			component2.anchoredPosition = new Vector2(18f, 0f);
			rowView._icon = MakeIcon(gameObject3.transform, "Icon");
			rowView._iconOverlay = MakeIcon(gameObject3.transform, "IconOverlay");
			rowView._name = MakeText(gameObject.transform, "ItemName", "", 24, TextAlignmentOptions.Left);
			rowView._name.fontStyle = FontStyles.Bold;
			RectTransform rectTransform = rowView._name.rectTransform;
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.offsetMin = new Vector2(78f, 0f);
			rectTransform.offsetMax = new Vector2(-180f, 0f);
			rowView._count = MakeText(gameObject.transform, "Count", "", 20, TextAlignmentOptions.Right);
			rowView._count.color = ColMuted;
			RectTransform rectTransform2 = rowView._count.rectTransform;
			rectTransform2.anchorMin = new Vector2(1f, 0f);
			rectTransform2.anchorMax = new Vector2(1f, 1f);
			rectTransform2.pivot = new Vector2(1f, 0.5f);
			rectTransform2.anchoredPosition = new Vector2(-128f, 0f);
			rectTransform2.sizeDelta = new Vector2(60f, 0f);
			rowView._take = MakeText(gameObject.transform, "TakeLabel", "Take", 22, TextAlignmentOptions.Center);
			rowView._take.fontStyle = FontStyles.Bold;
			rowView._take.color = ColAccent;
			RectTransform rectTransform3 = rowView._take.rectTransform;
			rectTransform3.anchorMin = new Vector2(1f, 0f);
			rectTransform3.anchorMax = new Vector2(1f, 1f);
			rectTransform3.pivot = new Vector2(1f, 0.5f);
			rectTransform3.anchoredPosition = new Vector2(-22f, 0f);
			rectTransform3.sizeDelta = new Vector2(92f, 0f);
			return rowView;
		}

		private static Image MakeIcon(Transform parent, string name)
		{
			Image image = NewGO(name, parent).AddComponent<Image>();
			image.preserveAspect = true;
			image.raycastTarget = false;
			image.enabled = false;
			RectTransform rectTransform = image.rectTransform;
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = new Vector2(5f, 5f);
			rectTransform.offsetMax = new Vector2(-5f, -5f);
			return image;
		}

		public void SetActive(bool on)
		{
			if (go.activeSelf != on)
			{
				go.SetActive(on);
			}
		}

		public void Populate(Chest.ChestItem item, UnityAction onClick)
		{
			_name.text = ((item != null) ? item.name : "Item");
			if (item != null && item.icon != null)
			{
				_icon.sprite = item.icon;
				_icon.color = ((item.iconTint.a <= 0f) ? Color.white : item.iconTint);
				_icon.enabled = true;
				if (item.iconOverlay != null)
				{
					_iconOverlay.sprite = item.iconOverlay;
					_iconOverlay.enabled = true;
				}
				else
				{
					_iconOverlay.enabled = false;
				}
			}
			else
			{
				_icon.enabled = false;
				_iconOverlay.enabled = false;
			}
			bool flag = item != null && item.count > 1;
			_count.gameObject.SetActive(flag);
			if (flag)
			{
				_count.text = "x" + item.count;
			}
			_btn.onClick.RemoveAllListeners();
			_btn.onClick.AddListener(onClick);
		}
	}

	private GameObject _root;

	private GameObject _panel;

	private Transform _itemsContent;

	private Chest _current;

	private PlayerController _player;

	private readonly List<RowView> _rows = new List<RowView>();

	private TextMeshProUGUI _emptyLabel;

	private static readonly Color ColBg = UIKit.Panel;

	private static readonly Color ColHeader = new Color(0.14f, 0.055f, 0.035f, 0.98f);

	private static readonly Color ColAccent = UIKit.Edge;

	private static readonly Color ColRow = new Color(0.1f, 0.045f, 0.03f, 1f);

	private static readonly Color ColRowHi = new Color(0.2f, 0.09f, 0.05f, 1f);

	private static readonly Color ColBtn = UIKit.BtnNormal;

	private static readonly Color ColBtnHi = new Color(0.2f, 0.1f, 0.06f, 1f);

	private static readonly Color ColBtnDn = new Color(0.45f, 0.22f, 0.08f, 1f);

	private static readonly Color ColText = UIKit.TextCol;

	private static readonly Color ColMuted = UIKit.Muted;

	public static ChestUI Instance { get; private set; }

	public bool IsOpen
	{
		get
		{
			if (_root != null)
			{
				return _root.activeSelf;
			}
			return false;
		}
	}

	private void Awake()
	{
		if (Instance != null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		BuildUI();
	}

	private void BuildUI()
	{
		GameObject gameObject = new GameObject("ChestUI_Canvas");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;
		CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.matchWidthOrHeight = 0.5f;
		gameObject.AddComponent<GraphicRaycaster>();
		_root = NewGO("Root", gameObject.transform);
		_root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.48f);
		Stretch(_root.GetComponent<RectTransform>());
		_panel = NewGO("Panel", _root.transform);
		_panel.AddComponent<Image>().color = ColBg;
		RectTransform component = _panel.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = new Vector2(0.5f, 0.5f));
		component.anchorMin = anchorMin;
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = new Vector2(620f, 610f);
		component.anchoredPosition = Vector2.zero;
		Outline outline = _panel.AddComponent<Outline>();
		outline.effectColor = ColAccent;
		outline.effectDistance = new Vector2(2f, -2f);
		GameObject obj = NewGO("Header", _panel.transform);
		obj.AddComponent<Image>().color = ColHeader;
		RectTransform component2 = obj.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.sizeDelta = new Vector2(0f, 74f);
		component2.anchoredPosition = Vector2.zero;
		TextMeshProUGUI textMeshProUGUI = MakeText(obj.transform, "Title", "CHEST", 42, TextAlignmentOptions.Center);
		textMeshProUGUI.fontStyle = FontStyles.Bold;
		textMeshProUGUI.color = ColAccent;
		Stretch(textMeshProUGUI.rectTransform);
		GameObject gameObject2 = NewGO("Items", _panel.transform);
		RectTransform component3 = gameObject2.GetComponent<RectTransform>();
		component3.anchorMin = new Vector2(0f, 0f);
		component3.anchorMax = new Vector2(1f, 1f);
		component3.pivot = new Vector2(0.5f, 0.5f);
		component3.offsetMin = new Vector2(28f, 126f);
		component3.offsetMax = new Vector2(-28f, -88f);
		ScrollRect scrollRect = gameObject2.AddComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 28f;
		GameObject gameObject3 = NewGO("Viewport", gameObject2.transform);
		Stretch(gameObject3.GetComponent<RectTransform>());
		gameObject3.AddComponent<RectMask2D>();
		GameObject gameObject4 = NewGO("Content", gameObject3.transform);
		RectTransform component4 = gameObject4.GetComponent<RectTransform>();
		component4.anchorMin = new Vector2(0f, 1f);
		component4.anchorMax = new Vector2(1f, 1f);
		component4.pivot = new Vector2(0.5f, 1f);
		component4.sizeDelta = Vector2.zero;
		VerticalLayoutGroup verticalLayoutGroup = gameObject4.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.spacing = 8f;
		verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
		verticalLayoutGroup.childForceExpandHeight = false;
		verticalLayoutGroup.childForceExpandWidth = true;
		verticalLayoutGroup.childControlHeight = true;
		verticalLayoutGroup.childControlWidth = true;
		gameObject4.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		scrollRect.viewport = gameObject3.GetComponent<RectTransform>();
		scrollRect.content = component4;
		_itemsContent = gameObject4.transform;
		MakeRectButton(_panel.transform, "TakeAll", "Take all", new Vector2(0.5f, 0f), new Vector2(-126f, 42f), new Vector2(230f, 48f), TakeAll);
		MakeRectButton(_panel.transform, "Close", "Close", new Vector2(0.5f, 0f), new Vector2(126f, 42f), new Vector2(230f, 48f), Close);
		_root.SetActive(value: false);
	}

	public void Open(Chest chest)
	{
		if (!(chest == null) && !(_root == null))
		{
			_current = chest;
			Refresh();
			_root.SetActive(value: true);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			if (GameManager.Instance != null)
			{
				GameManager.Instance.SetPaused(paused: true);
			}
			else
			{
				Time.timeScale = 0f;
			}
		}
	}

	public void Close()
	{
		if (_root != null)
		{
			_root.SetActive(value: false);
		}
		_current = null;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SetPaused(paused: false);
		}
		else
		{
			Time.timeScale = 1f;
		}
	}

	private void Update()
	{
		if (!NoteUI.IsOpen && _root != null && _root.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			Close();
		}
	}

	private void TakeAll()
	{
		if (!(_current == null))
		{
			_current.TakeAll();
			Refresh();
		}
	}

	private void Refresh()
	{
		if (_itemsContent == null)
		{
			return;
		}
		int num = ((_current != null && _current.items != null) ? _current.items.Count : 0);
		EnsureEmptyLabel();
		_emptyLabel.gameObject.SetActive(_current != null && num == 0);
		while (_rows.Count < num)
		{
			_rows.Add(RowView.Create(_itemsContent));
		}
		for (int i = 0; i < _rows.Count; i++)
		{
			if (i < num)
			{
				int idx = i;
				_rows[i].Populate(_current.items[i], delegate
				{
					if (!(_current == null))
					{
						_current.TakeItem(idx);
						Refresh();
					}
				});
				_rows[i].SetActive(on: true);
			}
			else
			{
				_rows[i].SetActive(on: false);
			}
		}
	}

	private void EnsureEmptyLabel()
	{
		if (!(_emptyLabel != null))
		{
			_emptyLabel = MakeText(_itemsContent, "Empty", "Empty", 28, TextAlignmentOptions.Center);
			_emptyLabel.color = ColMuted;
			_emptyLabel.fontStyle = FontStyles.Italic;
			_emptyLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
			_emptyLabel.gameObject.SetActive(value: false);
		}
	}

	private static GameObject NewGO(string name, Transform parent)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		return obj;
	}

	private static void Stretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	private static TextMeshProUGUI MakeText(Transform parent, string name, string text, int size, TextAlignmentOptions align)
	{
		TextMeshProUGUI textMeshProUGUI = NewGO(name, parent).AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.text = text;
		textMeshProUGUI.fontSize = size;
		textMeshProUGUI.alignment = align;
		textMeshProUGUI.color = ColText;
		textMeshProUGUI.textWrappingMode = TextWrappingModes.NoWrap;
		textMeshProUGUI.raycastTarget = false;
		return textMeshProUGUI;
	}

	private static Button MakeRectButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick)
	{
		GameObject obj = NewGO(name, parent);
		Image image = obj.AddComponent<Image>();
		image.color = ColBtn;
		RectTransform component = obj.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = anchor);
		component.anchorMin = anchorMin;
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = size;
		component.anchoredPosition = pos;
		Button button = obj.AddComponent<Button>();
		StyleButton(button, image);
		TextMeshProUGUI textMeshProUGUI = MakeText(obj.transform, "Label", label, 26, TextAlignmentOptions.Center);
		textMeshProUGUI.fontStyle = FontStyles.Bold;
		Stretch(textMeshProUGUI.rectTransform);
		button.onClick.AddListener(onClick);
		return button;
	}

	private static void StyleButton(Button btn, Image img)
	{
		StyleButton(btn, img, ColBtn, ColBtnHi, ColAccent);
	}

	private static void StyleButton(Button btn, Image img, Color normal, Color highlighted)
	{
		StyleButton(btn, img, normal, highlighted, new Color(0f, 0f, 0f, 0.6f));
	}

	private static void StyleButton(Button btn, Image img, Color normal, Color highlighted, Color outlineColor)
	{
		ColorBlock colors = btn.colors;
		colors.normalColor = normal;
		colors.highlightedColor = highlighted;
		colors.pressedColor = ColBtnDn;
		colors.selectedColor = highlighted;
		colors.fadeDuration = 0.1f;
		btn.colors = colors;
		btn.targetGraphic = img;
		Outline outline = img.gameObject.AddComponent<Outline>();
		outline.effectColor = outlineColor;
		outline.effectDistance = new Vector2(2f, -2f);
	}
}
