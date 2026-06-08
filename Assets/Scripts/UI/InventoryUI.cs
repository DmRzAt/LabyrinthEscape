using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
	public Key toggleKey = Key.I;

	private static readonly Color ColBg = UIKit.Panel;

	private static readonly Color ColHeader = new Color(0.14f, 0.055f, 0.035f, 0.98f);

	private static readonly Color ColAccent = UIKit.Edge;

	private static readonly Color ColSlot = new Color(0.1f, 0.045f, 0.03f, 1f);

	private static readonly Color ColSlotHi = new Color(0.22f, 0.1f, 0.05f, 1f);

	private static readonly Color ColText = UIKit.TextCol;

	private GameObject _root;

	private Transform _itemsGrid;

	private TextMeshProUGUI _hint;

	private TextMeshProUGUI _weightLabel;

	private PlayerInventory _inv;

	private PlayerStats _stats;

	private PlayerController _controller;

	private InventoryItem _selectedItem;

	private Button _readBtn;

	private GameObject _tooltip;

	private TextMeshProUGUI _tooltipText;

	private RectTransform _tooltipRT;

	public static bool IsOpen { get; private set; }

	private void Awake()
	{
		BuildUI();
		PlayerInventory.OnInventoryChanged += Refresh;
		PlayerInventory.OnHotbarSlotChanged += OnHotbarSlotChanged;
	}

	private void OnDestroy()
	{
		PlayerInventory.OnInventoryChanged -= Refresh;
		PlayerInventory.OnHotbarSlotChanged -= OnHotbarSlotChanged;
	}

	private void OnHotbarSlotChanged(int idx, InventoryItem item)
	{
		Refresh();
	}

	private void Start()
	{
		_inv = PlayerInventory.Instance;
		if (_inv != null)
		{
			_stats = _inv.GetComponent<PlayerStats>();
			_controller = _inv.GetComponent<PlayerController>();
		}
		Close();
	}

	private void Update()
	{
		if (_root == null || NoteUI.IsOpen)
		{
			return;
		}
		Keyboard current = Keyboard.current;
		if (current == null)
		{
			return;
		}
		if (current[toggleKey].wasPressedThisFrame)
		{
			if (_root.activeSelf)
			{
				Close();
			}
			else if (!MazeMap.IsOpen && (GameManager.Instance == null || !GameManager.Instance.IsPaused))
			{
				Open();
			}
		}
		else if (_root.activeSelf && current.escapeKey.wasPressedThisFrame)
		{
			Close();
		}
	}

	public void Open()
	{
		if (!MazeMap.IsOpen && (!(GameManager.Instance != null) || !GameManager.Instance.IsPaused))
		{
			if (_inv == null)
			{
				_inv = PlayerInventory.Instance;
			}
			Refresh();
			if (_root != null)
			{
				_root.SetActive(value: true);
			}
			IsOpen = true;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			if (GameManager.Instance != null)
			{
				GameManager.Instance.SetPaused(paused: true);
			}
		}
	}

	public void Close()
	{
		HideTooltip();
		if (_root != null)
		{
			_root.SetActive(value: false);
		}
		IsOpen = false;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		if (GameManager.Instance != null)
		{
			GameManager.Instance.SetPaused(paused: false);
		}
	}

	private void BuildUI()
	{
		GameObject gameObject = new GameObject("InventoryUI_Canvas");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 90;
		CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		gameObject.AddComponent<GraphicRaycaster>();
		_root = NewGO("Root", gameObject.transform);
		_root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
		Stretch(_root.GetComponent<RectTransform>());
		GameObject gameObject2 = NewGO("Panel", _root.transform);
		gameObject2.AddComponent<Image>().color = ColBg;
		RectTransform component = gameObject2.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = new Vector2(0.5f, 0.5f));
		component.anchorMin = anchorMin;
		component.pivot = new Vector2(0.5f, 0.5f);
		component.sizeDelta = new Vector2(800f, 600f);
		Outline outline = gameObject2.AddComponent<Outline>();
		outline.effectColor = ColAccent;
		outline.effectDistance = new Vector2(3f, -3f);
		GameObject gameObject3 = NewGO("Header", gameObject2.transform);
		gameObject3.AddComponent<Image>().color = ColHeader;
		RectTransform component2 = gameObject3.GetComponent<RectTransform>();
		component2.anchorMin = new Vector2(0f, 1f);
		component2.anchorMax = new Vector2(1f, 1f);
		component2.pivot = new Vector2(0.5f, 1f);
		component2.sizeDelta = new Vector2(0f, 70f);
		component2.anchoredPosition = Vector2.zero;
		TextMeshProUGUI textMeshProUGUI = MakeText(gameObject3.transform, "Title", "INVENTORY", 40, TextAlignmentOptions.Center);
		textMeshProUGUI.fontStyle = FontStyles.Bold;
		textMeshProUGUI.color = ColAccent;
		Stretch(textMeshProUGUI.rectTransform);
		_weightLabel = MakeText(gameObject3.transform, "Weight", "", 18, TextAlignmentOptions.Right);
		_weightLabel.fontStyle = FontStyles.Bold;
		_weightLabel.raycastTarget = false;
		RectTransform rectTransform = _weightLabel.rectTransform;
		rectTransform.anchorMin = new Vector2(1f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(1f, 0.5f);
		rectTransform.anchoredPosition = new Vector2(-20f, 0f);
		rectTransform.sizeDelta = new Vector2(260f, 30f);
		_hint = MakeText(gameObject2.transform, "Hint", "Click an item, then press a number to assign to hotbar.   [I] to close", 18, TextAlignmentOptions.Center);
		_hint.color = UIKit.Muted;
		RectTransform rectTransform2 = _hint.rectTransform;
		rectTransform2.anchorMin = new Vector2(0f, 0f);
		rectTransform2.anchorMax = new Vector2(1f, 0f);
		rectTransform2.pivot = new Vector2(0.5f, 0f);
		rectTransform2.sizeDelta = new Vector2(0f, 30f);
		rectTransform2.anchoredPosition = new Vector2(0f, 12f);
		UIKit.Button(gameObject2.transform, -160f, -210f, 150f, 44f, "Unbind", delegate
		{
			if (_selectedItem != null && !(_inv == null))
			{
				_inv.RemoveFromHotbar(_selectedItem);
				Refresh();
			}
		});
		_readBtn = UIKit.Button(gameObject2.transform, 0f, -210f, 150f, 44f, "Read", delegate
		{
			if (_selectedItem != null && _selectedItem.kind == InventoryItem.ItemKind.Note)
			{
				NoteUI.Show(_selectedItem.displayName, _selectedItem.noteText, _selectedItem.icon);
			}
		});
		_readBtn.gameObject.SetActive(value: false);
		UIKit.Button(gameObject2.transform, 160f, -210f, 150f, 44f, "Drop", delegate
		{
			if (_selectedItem != null && !(_inv == null))
			{
				InventoryItem item = _selectedItem;
				UIKit.Confirm(_root.transform, "Drop " + item.displayName + "?", delegate
				{
					_inv.DropItemToWorld(item);
					if (_selectedItem == item)
					{
						_selectedItem = null;
					}
					Refresh();
				});
			}
		});
		GameObject gameObject4 = NewGO("Grid", gameObject2.transform);
		RectTransform component3 = gameObject4.GetComponent<RectTransform>();
		component3.anchorMin = new Vector2(0f, 0f);
		component3.anchorMax = new Vector2(1f, 1f);
		component3.offsetMin = new Vector2(30f, 130f);
		component3.offsetMax = new Vector2(-30f, -90f);
		GridLayoutGroup gridLayoutGroup = gameObject4.AddComponent<GridLayoutGroup>();
		gridLayoutGroup.cellSize = new Vector2(110f, 110f);
		gridLayoutGroup.spacing = new Vector2(10f, 10f);
		gridLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
		_itemsGrid = gameObject4.transform;
		BuildTooltip(gameObject.transform);
	}

	private void BuildTooltip(Transform canvasParent)
	{
		_tooltip = NewGO("Tooltip", canvasParent);
		_tooltipRT = _tooltip.GetComponent<RectTransform>();
		_tooltipRT.pivot = new Vector2(0f, 1f);
		_tooltipRT.sizeDelta = new Vector2(300f, 64f);
		Image image = _tooltip.AddComponent<Image>();
		image.color = new Color(0.06f, 0.03f, 0.02f, 0.97f);
		image.raycastTarget = false;
		Outline outline = _tooltip.AddComponent<Outline>();
		outline.effectColor = ColAccent;
		outline.effectDistance = new Vector2(2f, -2f);
		_tooltipText = MakeText(_tooltip.transform, "Text", "", 17, TextAlignmentOptions.TopLeft);
		_tooltipText.raycastTarget = false;
		_tooltipText.textWrappingMode = TextWrappingModes.Normal;
		RectTransform rectTransform = _tooltipText.rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = new Vector2(10f, 8f);
		rectTransform.offsetMax = new Vector2(-10f, -8f);
		_tooltip.SetActive(value: false);
	}

	private void Refresh()
	{
		if (_itemsGrid == null)
		{
			return;
		}
		HideTooltip();
		UpdateWeight();
		for (int num = _itemsGrid.childCount - 1; num >= 0; num--)
		{
			GameObject obj = _itemsGrid.GetChild(num).gameObject;
			obj.transform.SetParent(null, worldPositionStays: false);
			Object.Destroy(obj);
		}
		if (_inv == null)
		{
			return;
		}
		if (_hint != null)
		{
			_hint.text = $"Click an item, then press 1–{_inv.hotbarSize} to assign to hotbar.   [I] to close";
		}
		int maxSlots = _inv.maxSlots;
		for (int i = 0; i < maxSlots; i++)
		{
			InventoryItem inventoryItem = ((i < _inv.items.Count) ? _inv.items[i] : null);
			bool flag = inventoryItem != null && inventoryItem == _selectedItem;
			GameObject gameObject = NewGO("Slot_" + i, _itemsGrid);
			Image image = gameObject.AddComponent<Image>();
			image.color = (flag ? ColSlotHi : ColSlot);
			Outline outline = gameObject.AddComponent<Outline>();
			outline.effectColor = (flag ? new Color(1f, 0.62f, 0.1f, 1f) : new Color(0f, 0f, 0f, 0.6f));
			outline.effectDistance = (flag ? new Vector2(5f, -5f) : new Vector2(2f, -2f));
			if (inventoryItem == null)
			{
				continue;
			}
			InventoryItem captured = inventoryItem;
			Button button = gameObject.AddComponent<Button>();
			ColorBlock colors = button.colors;
			colors.normalColor = ((inventoryItem == _selectedItem) ? ColSlotHi : ColSlot);
			colors.highlightedColor = ColSlotHi;
			button.colors = colors;
			button.targetGraphic = image;
			button.onClick.AddListener(delegate
			{
				_selectedItem = captured;
				Refresh();
			});
			string desc = Describe(captured);
			EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerEnter
			};
			entry.callback.AddListener(delegate
			{
				ShowTooltip(captured.displayName, desc);
			});
			eventTrigger.triggers.Add(entry);
			EventTrigger.Entry entry2 = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerExit
			};
			entry2.callback.AddListener(delegate
			{
				HideTooltip();
			});
			eventTrigger.triggers.Add(entry2);
			for (int j = 0; j < _inv.hotbarSize; j++)
			{
				if (_inv.hotbar[j] == inventoryItem)
				{
					TextMeshProUGUI textMeshProUGUI = MakeText(gameObject.transform, "Bind", (j + 1).ToString(), 22, TextAlignmentOptions.TopRight);
					textMeshProUGUI.color = ColAccent;
					textMeshProUGUI.fontStyle = FontStyles.Bold;
					RectTransform rectTransform = textMeshProUGUI.rectTransform;
					rectTransform.anchorMin = new Vector2(1f, 1f);
					rectTransform.anchorMax = new Vector2(1f, 1f);
					rectTransform.pivot = new Vector2(1f, 1f);
					rectTransform.anchoredPosition = new Vector2(-5f, -3f);
					rectTransform.sizeDelta = new Vector2(25f, 25f);
					textMeshProUGUI.raycastTarget = false;
				}
			}
			if (inventoryItem.icon != null)
			{
				Color tint = ((inventoryItem.iconTint.a <= 0f) ? Color.white : inventoryItem.iconTint);
				AddIcon(gameObject.transform, inventoryItem.icon, tint, inventoryItem.iconOverlay, new Vector2(10f, 25f), new Vector2(-10f, -10f));
			}
			if (inventoryItem.count > 1)
			{
				TextMeshProUGUI textMeshProUGUI2 = MakeText(gameObject.transform, "Count", "x" + inventoryItem.count, 16, TextAlignmentOptions.TopLeft);
				textMeshProUGUI2.fontStyle = FontStyles.Bold;
				textMeshProUGUI2.color = Color.white;
				textMeshProUGUI2.raycastTarget = false;
				Outline outline2 = textMeshProUGUI2.gameObject.AddComponent<Outline>();
				outline2.effectColor = new Color(0f, 0f, 0f, 0.85f);
				outline2.effectDistance = new Vector2(1.5f, -1.5f);
				RectTransform rectTransform2 = textMeshProUGUI2.rectTransform;
				rectTransform2.anchorMin = new Vector2(0f, 1f);
				rectTransform2.anchorMax = new Vector2(0f, 1f);
				rectTransform2.pivot = new Vector2(0f, 1f);
				rectTransform2.anchoredPosition = new Vector2(6f, -5f);
				rectTransform2.sizeDelta = new Vector2(50f, 22f);
			}
			TextMeshProUGUI textMeshProUGUI3 = MakeText(gameObject.transform, "Name", inventoryItem.displayName, 14, TextAlignmentOptions.Bottom);
			RectTransform rectTransform3 = textMeshProUGUI3.rectTransform;
			rectTransform3.anchorMin = new Vector2(0f, 0f);
			rectTransform3.anchorMax = new Vector2(1f, 0f);
			rectTransform3.pivot = new Vector2(0.5f, 0f);
			rectTransform3.anchoredPosition = new Vector2(0f, 5f);
			rectTransform3.sizeDelta = new Vector2(0f, 18f);
			textMeshProUGUI3.raycastTarget = false;
		}
		if (_readBtn != null)
		{
			_readBtn.gameObject.SetActive(_selectedItem != null && _selectedItem.kind == InventoryItem.ItemKind.Note);
		}
	}

	private void LateUpdate()
	{
		if (_tooltip != null && _tooltip.activeSelf)
		{
			Mouse current = Mouse.current;
			if (current != null)
			{
				Vector2 vector = current.position.ReadValue();
				_tooltipRT.position = new Vector3(vector.x + 16f, vector.y - 16f, 0f);
			}
		}
		if (_root == null || !_root.activeSelf || _selectedItem == null || _inv == null || NoteUI.IsOpen)
		{
			return;
		}
		Keyboard current2 = Keyboard.current;
		if (current2 == null)
		{
			return;
		}
		for (int i = 0; i < _inv.hotbarSize && i < 9; i++)
		{
			if (current2[(Key)(41 + i)].wasPressedThisFrame)
			{
				_inv.AssignToHotbar(_selectedItem, i);
				Refresh();
				break;
			}
		}
	}

	private static void AddIcon(Transform parent, Sprite baseSprite, Color tint, Sprite overlay, Vector2 insetMin, Vector2 insetMax)
	{
		Image image = NewGO("Icon", parent).AddComponent<Image>();
		image.sprite = baseSprite;
		image.color = tint;
		image.preserveAspect = true;
		image.raycastTarget = false;
		RectTransform rectTransform = image.rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = insetMin;
		rectTransform.offsetMax = insetMax;
		if (!(overlay == null))
		{
			Image image2 = NewGO("IconOverlay", parent).AddComponent<Image>();
			image2.sprite = overlay;
			image2.preserveAspect = true;
			image2.raycastTarget = false;
			RectTransform rectTransform2 = image2.rectTransform;
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.offsetMin = insetMin;
			rectTransform2.offsetMax = insetMax;
		}
	}

	private void UpdateWeight()
	{
		if (!(_weightLabel == null) && !(_inv == null))
		{
			float num = _inv.TotalWeight + ((_stats != null) ? _stats.armorWeight : 0f);
			float num2 = ((_stats != null) ? _stats.CarryCapacity : 10f);
			float num3 = ((_controller != null) ? (num2 + (_controller.heavyMaxWeight - _controller.lightMaxWeight)) : num2);
			_weightLabel.text = $"Weight: {num:0.#} / {num2:0.#}";
			if (num > num3)
			{
				_weightLabel.color = new Color(1f, 0.3f, 0.25f, 1f);
			}
			else if (num > num2)
			{
				_weightLabel.color = new Color(1f, 0.62f, 0.1f, 1f);
			}
			else
			{
				_weightLabel.color = ColText;
			}
		}
	}

	private void ShowTooltip(string title, string body)
	{
		if (!(_tooltip == null))
		{
			string text = (string.IsNullOrEmpty(body) ? ("<b>" + title + "</b>") : ("<b>" + title + "</b>\n" + body));
			_tooltipText.text = text;
			float y = _tooltipText.GetPreferredValues(text, 280f, 0f).y;
			_tooltipRT.sizeDelta = new Vector2(300f, Mathf.Ceil(y) + 16f);
			_tooltip.SetActive(value: true);
			_tooltip.transform.SetAsLastSibling();
		}
	}

	private void HideTooltip()
	{
		if (_tooltip != null)
		{
			_tooltip.SetActive(value: false);
		}
	}

	private static string Describe(InventoryItem it)
	{
		if (it == null)
		{
			return "";
		}
		switch (it.kind)
		{
		case InventoryItem.ItemKind.Potion:
			return it.potionEffect switch
			{
				InventoryItem.PotionEffect.Heal => $"Restores {Mathf.RoundToInt(it.potionMagnitude)} HP instantly.  (Q to drink)", 
				InventoryItem.PotionEffect.Regen => $"Regenerates {it.potionMagnitude:0.#} HP/s for {it.potionDuration:0}s.  (Q to drink)", 
				InventoryItem.PotionEffect.Speed => $"Movement speed x{it.potionMagnitude:0.##} for {it.potionDuration:0}s.  (Q to drink)", 
				InventoryItem.PotionEffect.Stamina => $"Restores {it.potionMagnitude:0.#} stamina/s for {it.potionDuration:0}s.  (Q to drink)", 
				InventoryItem.PotionEffect.Jump => $"Jump height x{it.potionMagnitude:0.##} for {it.potionDuration:0}s.  (Q to drink)", 
				_ => "Drink with Q.", 
			};
		case InventoryItem.ItemKind.Sword:
		{
			WeaponStats.Stat stat = WeaponStats.Get(it.displayName);
			return $"Weapon — Damage {stat.damage}, Speed {WeaponStats.SpeedLabel(stat.speed)}.\nAssign to a hotbar slot to wield.";
		}
		case InventoryItem.ItemKind.Note:
			return "Readable note. Select it, then press Read.";
		case InventoryItem.ItemKind.Key:
			return "Opens a locked door.";
		default:
			return "";
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
		return textMeshProUGUI;
	}
}
