using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
	[Header("Interaction")]
	[SerializeField]
	[FormerlySerializedAs("interactDistance")]
	[Range(0.5f, 6f)]
	private float _interactDistance = 2.5f;

	[SerializeField]
	[FormerlySerializedAs("cameraTransform")]
	private Transform _cameraTransform;

	[SerializeField]
	[FormerlySerializedAs("hintText")]
	private TextMeshProUGUI _hintText;

	[SerializeField]
	private bool _createHintIfMissing = true;

	[SerializeField]
	private bool _debugRaycast;

	[SerializeField]
	private LayerMask _interactMask = -1;

	[SerializeField]
	private Vector2 _hintBottomCenterOffset = new Vector2(0f, 185f);

	[SerializeField]
	private Vector2 _hintSize = new Vector2(720f, 64f);

	[SerializeField]
	private int _hintSortingOrder = 100;

	private IInteractable _current;

	private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

	private void Awake()
	{
		if (_cameraTransform == null && Camera.main != null)
		{
			_cameraTransform = Camera.main.transform;
		}
		if (_hintText == null && _createHintIfMissing)
		{
			_hintText = CreateHintText();
		}
		ConfigureHintLayout(_hintText);
	}

	private void Update()
	{
		if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked)
		{
			_current = null;
			SetHintVisible(isVisible: false);
			return;
		}
		if (_cameraTransform == null)
		{
			SetHintVisible(isVisible: false);
			return;
		}
		Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
		_current = GetNearestInteractable(ray);
		if (_debugRaycast)
		{
			Debug.Log($"[Interaction] Selected interactable: {_current != null}");
		}
		if (_current != null)
		{
			SetHintText(_current.Prompt + "  [E]");
			SetHintVisible(isVisible: true);
		}
		else
		{
			SetHintVisible(isVisible: false);
		}
		if (_current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
		{
			_current.Interact();
		}
	}

	private IInteractable GetNearestInteractable(Ray ray)
	{
		int num = Physics.RaycastNonAlloc(ray, _hitBuffer, _interactDistance, _interactMask, QueryTriggerInteraction.Collide);
		IInteractable result = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = _hitBuffer[i];
			if (!(raycastHit.distance >= num2) && !(raycastHit.collider.transform == base.transform) && !raycastHit.collider.transform.IsChildOf(base.transform))
			{
				IInteractable componentInParent = raycastHit.collider.GetComponentInParent<IInteractable>();
				if (!raycastHit.collider.isTrigger || componentInParent != null)
				{
					result = componentInParent;
					num2 = raycastHit.distance;
				}
			}
		}
		return result;
	}

	private void SetHintText(string text)
	{
		if (_hintText != null)
		{
			_hintText.text = text;
		}
	}

	private void SetHintVisible(bool isVisible)
	{
		if (_hintText != null)
		{
			_hintText.gameObject.SetActive(isVisible);
		}
	}

	private TextMeshProUGUI CreateHintText()
	{
		Canvas canvas = FindInteractionHintCanvas();
		if (canvas == null)
		{
			canvas = CreateInteractionHintCanvas();
		}
		GameObject gameObject = new GameObject("InteractionHint_Text");
		gameObject.transform.SetParent(canvas.transform, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.text = string.Empty;
		textMeshProUGUI.fontSize = 28f;
		textMeshProUGUI.alignment = TextAlignmentOptions.Center;
		textMeshProUGUI.color = Color.white;
		textMeshProUGUI.raycastTarget = false;
		gameObject.SetActive(value: false);
		return textMeshProUGUI;
	}

	private Canvas FindInteractionHintCanvas()
	{
		Canvas[] array = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
		foreach (Canvas canvas in array)
		{
			if (canvas.name == "InteractionHint_Canvas" && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				return canvas;
			}
		}
		return null;
	}

	private Canvas CreateInteractionHintCanvas()
	{
		GameObject obj = new GameObject("InteractionHint_Canvas");
		Canvas canvas = obj.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = _hintSortingOrder;
		CanvasScaler canvasScaler = obj.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		return canvas;
	}

	private void ConfigureHintLayout(TextMeshProUGUI text)
	{
		if (!(text == null))
		{
			Canvas componentInParent = text.GetComponentInParent<Canvas>();
			if (componentInParent != null && componentInParent.name == "InteractionHint_Canvas")
			{
				componentInParent.sortingOrder = _hintSortingOrder;
			}
			text.alignment = TextAlignmentOptions.Center;
			text.raycastTarget = false;
			RectTransform rectTransform = text.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 0f);
			rectTransform.anchorMax = new Vector2(0.5f, 0f);
			rectTransform.pivot = new Vector2(0.5f, 0f);
			rectTransform.anchoredPosition = _hintBottomCenterOffset;
			rectTransform.sizeDelta = _hintSize;
		}
	}
}
