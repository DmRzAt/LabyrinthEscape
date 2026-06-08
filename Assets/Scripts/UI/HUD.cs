using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
	private class BuffChip
	{
		public PlayerStatusEffects.Effect effect;

		public Image fill;

		public TextMeshProUGUI timeText;
	}

	[Header("HP")]
	public Slider hpSlider;

	public TextMeshProUGUI hpText;

	[Header("Stamina")]
	public Slider staminaSlider;

	public TextMeshProUGUI staminaText;

	[Header("Keys")]
	public TextMeshProUGUI keysText;

	[Header("Messages")]
	public TextMeshProUGUI messageText;

	[Header("Vitals Layout")]
	[SerializeField]
	private Vector2 _vitalsPanelOffset = new Vector2(30f, 36f);

	[SerializeField]
	private Vector2 _vitalsPanelSize = new Vector2(350f, 104f);

	[SerializeField]
	private Vector2 _barSize = new Vector2(300f, 14f);

	[SerializeField]
	private float _labelFontSize = 18f;

	private static readonly Color ColPanel = new Color(0.035f, 0.025f, 0.02f, 0f);

	private static readonly Color ColPanelEdge = new Color(0.78f, 0.55f, 0.25f, 0.85f);

	private static readonly Color ColBarBack = new Color(0.015f, 0.012f, 0.01f, 0.9f);

	private static readonly Color ColText = new Color(0.95f, 0.9f, 0.78f, 1f);

	private static readonly Color ColHealthHigh = new Color(0.82f, 0.08f, 0.07f, 1f);

	private static readonly Color ColHealthLow = new Color(0.42f, 0.02f, 0.02f, 1f);

	private static readonly Color ColStaminaHigh = new Color(0.86f, 0.68f, 0.28f, 1f);

	private static readonly Color ColStaminaLow = new Color(0.35f, 0.22f, 0.08f, 1f);

	private Image _hpFill;

	private Image _staminaFill;

	private bool _styled;

	private static readonly Color ColBuffBack = new Color(0.05f, 0.04f, 0.035f, 0.92f);

	private PlayerStatusEffects _statusEffects;

	private Transform _buffsRoot;

	private readonly List<BuffChip> _chips = new List<BuffChip>();

	private void OnEnable()
	{
		PlayerHealth.OnHealthChanged += UpdateHP;
		PlayerStamina.OnStaminaChanged += UpdateStamina;
		GameManager.OnKeysChanged += UpdateKeys;
		GameManager.OnGameWon += ShowWin;
		GameManager.OnGameLost += ShowLose;
		PlayerStatusEffects.OnEffectsChanged += RebuildBuffs;
	}

	private void Start()
	{
		StyleVitals();
		if (GameManager.Instance != null)
		{
			UpdateKeys(GameManager.Instance.keysCollected, GameManager.Instance.keysTotal);
		}
		PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
		if (playerHealth != null)
		{
			UpdateHP(playerHealth.currentHP, playerHealth.maxHP);
		}
		PlayerStamina playerStamina = Object.FindFirstObjectByType<PlayerStamina>();
		if (playerStamina != null)
		{
			UpdateStamina(playerStamina.currentStamina, playerStamina.EffectiveMax);
		}
		_statusEffects = Object.FindFirstObjectByType<PlayerStatusEffects>();
		BuildBuffsRoot();
		RebuildBuffs();
	}

	private void BuildBuffsRoot()
	{
		if (!(_buffsRoot != null))
		{
			Transform parent = GetVitalsParent() ?? base.transform;
			GameObject gameObject = new GameObject("Buffs", typeof(RectTransform));
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform obj = (RectTransform)gameObject.transform;
			obj.anchorMin = new Vector2(0f, 0f);
			obj.anchorMax = new Vector2(0f, 0f);
			obj.pivot = new Vector2(0f, 0f);
			obj.anchoredPosition = new Vector2(0f, 110f);
			obj.sizeDelta = new Vector2(240f, 0f);
			VerticalLayoutGroup verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.spacing = 6f;
			verticalLayoutGroup.childAlignment = TextAnchor.LowerLeft;
			verticalLayoutGroup.childForceExpandWidth = true;
			verticalLayoutGroup.childForceExpandHeight = false;
			verticalLayoutGroup.childControlWidth = true;
			verticalLayoutGroup.childControlHeight = true;
			gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			_buffsRoot = gameObject.transform;
		}
	}

	private void RebuildBuffs()
	{
		if (_buffsRoot == null)
		{
			return;
		}
		for (int num = _buffsRoot.childCount - 1; num >= 0; num--)
		{
			GameObject obj = _buffsRoot.GetChild(num).gameObject;
			obj.transform.SetParent(null, worldPositionStays: false);
			Object.Destroy(obj);
		}
		_chips.Clear();
		if (!(_statusEffects == null))
		{
			IReadOnlyList<PlayerStatusEffects.Effect> active = _statusEffects.Active;
			for (int i = 0; i < active.Count; i++)
			{
				_chips.Add(MakeChip(active[i]));
			}
		}
	}

	private BuffChip MakeChip(PlayerStatusEffects.Effect e)
	{
		GameObject gameObject = new GameObject("Buff", typeof(RectTransform));
		gameObject.transform.SetParent(_buffsRoot, worldPositionStays: false);
		gameObject.AddComponent<Image>().color = ColBuffBack;
		gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
		Outline outline = gameObject.AddComponent<Outline>();
		outline.effectColor = new Color(e.color.r, e.color.g, e.color.b, 0.85f);
		outline.effectDistance = new Vector2(2f, -2f);
		Image image = new GameObject("Accent", typeof(RectTransform)).AddComponent<Image>();
		image.transform.SetParent(gameObject.transform, worldPositionStays: false);
		image.color = e.color;
		image.raycastTarget = false;
		RectTransform rectTransform = image.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(0f, 1f);
		rectTransform.pivot = new Vector2(0f, 0.5f);
		rectTransform.sizeDelta = new Vector2(5f, 0f);
		rectTransform.anchoredPosition = Vector2.zero;
		GameObject obj = new GameObject("Fill", typeof(RectTransform));
		obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
		Image image2 = obj.AddComponent<Image>();
		image2.color = new Color(e.color.r, e.color.g, e.color.b, 0.55f);
		image2.raycastTarget = false;
		RectTransform rectTransform2 = image2.rectTransform;
		rectTransform2.anchorMin = new Vector2(0f, 0f);
		rectTransform2.anchorMax = new Vector2(1f, 0f);
		rectTransform2.pivot = new Vector2(0f, 0f);
		rectTransform2.sizeDelta = new Vector2(0f, 3f);
		rectTransform2.anchoredPosition = Vector2.zero;
		TextMeshProUGUI textMeshProUGUI = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.transform.SetParent(gameObject.transform, worldPositionStays: false);
		textMeshProUGUI.text = e.label;
		textMeshProUGUI.fontSize = 15f;
		textMeshProUGUI.fontStyle = FontStyles.Bold;
		textMeshProUGUI.alignment = TextAlignmentOptions.Left;
		textMeshProUGUI.color = ColText;
		textMeshProUGUI.raycastTarget = false;
		textMeshProUGUI.textWrappingMode = TextWrappingModes.NoWrap;
		textMeshProUGUI.overflowMode = TextOverflowModes.Ellipsis;
		RectTransform rectTransform3 = textMeshProUGUI.rectTransform;
		rectTransform3.anchorMin = Vector2.zero;
		rectTransform3.anchorMax = Vector2.one;
		rectTransform3.offsetMin = new Vector2(14f, 0f);
		rectTransform3.offsetMax = new Vector2(-48f, 0f);
		TextMeshProUGUI textMeshProUGUI2 = new GameObject("Time", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
		textMeshProUGUI2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		textMeshProUGUI2.fontSize = 15f;
		textMeshProUGUI2.fontStyle = FontStyles.Bold;
		textMeshProUGUI2.alignment = TextAlignmentOptions.Right;
		textMeshProUGUI2.color = e.color;
		textMeshProUGUI2.raycastTarget = false;
		textMeshProUGUI2.textWrappingMode = TextWrappingModes.NoWrap;
		RectTransform rectTransform4 = textMeshProUGUI2.rectTransform;
		rectTransform4.anchorMin = new Vector2(1f, 0f);
		rectTransform4.anchorMax = new Vector2(1f, 1f);
		rectTransform4.pivot = new Vector2(1f, 0.5f);
		rectTransform4.sizeDelta = new Vector2(46f, 0f);
		rectTransform4.anchoredPosition = new Vector2(-10f, 0f);
		return new BuffChip
		{
			effect = e,
			fill = image2,
			timeText = textMeshProUGUI2
		};
	}

	private void OnDisable()
	{
		PlayerHealth.OnHealthChanged -= UpdateHP;
		PlayerStamina.OnStaminaChanged -= UpdateStamina;
		GameManager.OnKeysChanged -= UpdateKeys;
		GameManager.OnGameWon -= ShowWin;
		GameManager.OnGameLost -= ShowLose;
		PlayerStatusEffects.OnEffectsChanged -= RebuildBuffs;
	}

	private void Update()
	{
		for (int i = 0; i < _chips.Count; i++)
		{
			BuffChip buffChip = _chips[i];
			if (buffChip.effect != null)
			{
				float num = Mathf.Max(0f, buffChip.effect.timeRemaining);
				buffChip.timeText.text = $"{Mathf.CeilToInt(num)}s";
				float x = ((buffChip.effect.duration > 0f) ? Mathf.Clamp01(num / buffChip.effect.duration) : 0f);
				buffChip.fill.rectTransform.anchorMax = new Vector2(x, 0f);
			}
		}
	}

	private void UpdateHP(int current, int max)
	{
		if (hpSlider != null)
		{
			hpSlider.maxValue = max;
			hpSlider.value = current;
		}
		if (hpText != null)
		{
			hpText.text = $"{current}/{max}";
		}
		SetFillColor(_hpFill, ColHealthLow, ColHealthHigh, current, max);
	}

	private void UpdateStamina(float current, float max)
	{
		if (staminaSlider != null)
		{
			staminaSlider.maxValue = max;
			staminaSlider.value = current;
		}
		if (staminaText != null)
		{
			staminaText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
		}
		SetFillColor(_staminaFill, ColStaminaLow, ColStaminaHigh, current, max);
	}

	private void UpdateKeys(int collected, int total)
	{
		if (keysText != null)
		{
			keysText.text = $"Keys {collected}/{total}";
		}
	}

	private void ShowWin()
	{
	}

	private void ShowLose()
	{
		ShowMessage("YOU DIED...");
	}

	private void ShowMessage(string msg)
	{
		if (messageText != null)
		{
			messageText.text = msg;
			messageText.gameObject.SetActive(value: true);
		}
	}

	private void StyleVitals()
	{
		if (_styled)
		{
			return;
		}
		_styled = true;
		Transform vitalsParent = GetVitalsParent();
		if (!(vitalsParent == null))
		{
			Image orCreateImage = GetOrCreateImage(vitalsParent, "Vitals_Frame");
			orCreateImage.color = ColPanel;
			orCreateImage.raycastTarget = false;
			ConfigureRect(orCreateImage.rectTransform, _vitalsPanelOffset, _vitalsPanelSize);
			orCreateImage.transform.SetAsFirstSibling();
			Outline component = orCreateImage.GetComponent<Outline>();
			if (component != null)
			{
				component.enabled = false;
			}
			GetOrCreateImage(orCreateImage.transform, "Vitals_Accent").enabled = false;
			Transform parent = orCreateImage.transform;
			if (staminaText == null)
			{
				staminaText = CreateLabel(parent, "StaminaText", hpText);
			}
			ConfigureLabel(hpText, parent, new Vector2(14f, 70f), new Vector2(300f, 22f));
			ConfigureLabel(staminaText, parent, new Vector2(14f, 30f), new Vector2(300f, 22f));
			_hpFill = ConfigureSlider(hpSlider, parent, new Vector2(14f, 54f), ColHealthHigh);
			_staminaFill = ConfigureSlider(staminaSlider, parent, new Vector2(14f, 14f), ColStaminaHigh);
		}
	}

	private Transform GetVitalsParent()
	{
		if (hpSlider != null)
		{
			return hpSlider.transform.parent;
		}
		if (staminaSlider != null)
		{
			return staminaSlider.transform.parent;
		}
		if (hpText != null)
		{
			return hpText.transform.parent;
		}
		return base.transform;
	}

	private Image ConfigureSlider(Slider slider, Transform parent, Vector2 position, Color fillColor)
	{
		if (slider == null)
		{
			return null;
		}
		slider.transform.SetParent(parent, worldPositionStays: false);
		slider.interactable = false;
		slider.transition = Selectable.Transition.None;
		slider.direction = Slider.Direction.LeftToRight;
		ConfigureRect((RectTransform)slider.transform, position, _barSize);
		Image image = slider.targetGraphic as Image;
		if (image == null)
		{
			image = slider.GetComponentInChildren<Image>();
		}
		if (image != null)
		{
			image.color = ColBarBack;
			image.raycastTarget = false;
			Outline obj = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
			obj.effectColor = new Color(0f, 0f, 0f, 0.9f);
			obj.effectDistance = new Vector2(2f, -2f);
		}
		Image image2 = ((slider.fillRect != null) ? slider.fillRect.GetComponent<Image>() : null);
		if (image2 != null)
		{
			image2.color = fillColor;
			image2.raycastTarget = false;
		}
		return image2;
	}

	private void ConfigureLabel(TextMeshProUGUI label, Transform parent, Vector2 position, Vector2 size)
	{
		if (!(label == null))
		{
			label.transform.SetParent(parent, worldPositionStays: false);
			label.fontSize = _labelFontSize;
			label.fontStyle = FontStyles.Bold;
			label.alignment = TextAlignmentOptions.Left;
			label.color = ColText;
			label.raycastTarget = false;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			ConfigureRect(label.rectTransform, position, size);
		}
	}

	private TextMeshProUGUI CreateLabel(Transform parent, string name, TextMeshProUGUI template)
	{
		GameObject obj = new GameObject(name, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI = obj.AddComponent<TextMeshProUGUI>();
		if (template != null && template.font != null)
		{
			textMeshProUGUI.font = template.font;
		}
		return textMeshProUGUI;
	}

	private Image GetOrCreateImage(Transform parent, string name)
	{
		Transform transform = parent.Find(name);
		if (transform != null && transform.TryGetComponent<Image>(out var component))
		{
			return component;
		}
		GameObject obj = new GameObject(name, typeof(RectTransform));
		obj.transform.SetParent(parent, worldPositionStays: false);
		return obj.AddComponent<Image>();
	}

	private void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(0f, 0f);
		rect.pivot = new Vector2(0f, 0f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private void SetFillColor(Image fill, Color low, Color high, float current, float max)
	{
		if (!(fill == null))
		{
			float t = ((max > 0f) ? Mathf.Clamp01(current / max) : 0f);
			fill.color = Color.Lerp(low, high, t);
		}
	}
}
