using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, FormerlySerializedAs("interactDistance"), Range(0.5f, 6f)] private float _interactDistance = 2.5f;
    [SerializeField, FormerlySerializedAs("cameraTransform")] private Transform _cameraTransform;
    [SerializeField, FormerlySerializedAs("hintText")] private TextMeshProUGUI _hintText;
    [SerializeField] private bool _createHintIfMissing = true;
    [SerializeField] private bool _debugRaycast = false;

    private IInteractable _current;

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
    }

    private void Update()
    {
        if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked)
        {
            _current = null;
            SetHintVisible(false);
            return;
        }

        if (_cameraTransform == null)
        {
            SetHintVisible(false);
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
            SetHintText($"{_current.Prompt}  [E]");
            SetHintVisible(true);
        }
        else
        {
            SetHintVisible(false);
        }

        if (_current != null && Input.GetKeyDown(KeyCode.E))
        {
            _current.Interact();
        }
    }

    private IInteractable GetNearestInteractable(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, _interactDistance);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                return interactable;
            }
        }

        return null;
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
        GameObject canvasObject = new GameObject("InteractionHint_Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject("InteractionHint_Text");
        textObject.transform.SetParent(canvasObject.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 120f);
        rectTransform.sizeDelta = new Vector2(600f, 60f);

        textObject.SetActive(false);
        return text;
    }
}
