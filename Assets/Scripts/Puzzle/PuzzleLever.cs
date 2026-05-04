using System;
using UnityEngine;

public class PuzzleLever : MonoBehaviour, IInteractable
{
    private enum RotationAxis { X, Y, Z }

    public event Action<PuzzleLever, bool> StateChanged;

    [Header("State")]
    [SerializeField] private bool _startsEnabled = false;
    [SerializeField] private bool _startsOn = false;
    [SerializeField] private bool _canToggleOff = true;
    [SerializeField] private bool _disableLegacyAnButton = true;
    [SerializeField] private bool _debugLogs = true;

    [Header("Prompt")]
    [SerializeField] private string _disabledPrompt = "Inactive Lever";
    [SerializeField] private string _offPrompt = "Pull Lever";
    [SerializeField] private string _onPrompt = "Lever On";

    [Header("Visuals")]
    [SerializeField] private Transform _handle;
    [SerializeField] private bool _rotateSelfIfHandleMissing = true;
    [SerializeField] private RotationAxis _rotationAxis = RotationAxis.X;
    [SerializeField, Range(-90f, 90f)] private float _offAngle = 0f;
    [SerializeField, Range(-90f, 90f)] private float _onAngle = 55f;
    [SerializeField, Range(1f, 20f)] private float _animationSpeed = 8f;
    [SerializeField] private Animator _animator;
    [SerializeField] private string _animatorBool = "LeverUp";

    public bool IsOn { get; private set; }
    public string Prompt => !_isEnabled ? _disabledPrompt : IsOn ? _onPrompt : _offPrompt;

    private bool _isEnabled;
    private Transform _visualTarget;
    private Quaternion _startLocalRotation;
    private Quaternion _targetLocalRotation;

    private void Awake()
    {
        if (_disableLegacyAnButton)
        {
            DisableLegacyAnButton();
        }

        _visualTarget = _handle != null ? _handle : _rotateSelfIfHandleMissing ? transform : null;
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator != null && _handle == null)
        {
            _visualTarget = null;
        }

        if (_visualTarget != null)
        {
            _startLocalRotation = _visualTarget.localRotation;
        }

        _isEnabled = _startsEnabled;
        IsOn = _startsOn;
        UpdateTargetRotation();
        ApplyVisualInstantly();
    }

    private void Update()
    {
        if (_visualTarget == null) return;

        _visualTarget.localRotation = Quaternion.Slerp(
            _visualTarget.localRotation,
            _targetLocalRotation,
            Time.deltaTime * _animationSpeed);
    }

    public void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled;
    }

    public void Interact()
    {
        if (_debugLogs)
        {
            Debug.Log($"[PuzzleLever] Interact: {name}, enabled={_isEnabled}, isOn={IsOn}", this);
        }

        if (!_isEnabled)
        {
            return;
        }

        if (IsOn && !_canToggleOff)
        {
            return;
        }

        SetOn(!IsOn);
    }

    public void SetOn(bool isOn, bool notify = true)
    {
        if (IsOn == isOn) return;

        IsOn = isOn;
        if (notify)
        {
            StateChanged?.Invoke(this, IsOn);
        }

        UpdateTargetRotation();
    }

    private void DisableLegacyAnButton()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "AN_Button")
            {
                behaviour.enabled = false;
            }
        }
    }

    private void UpdateTargetRotation()
    {
        if (_animator != null && !string.IsNullOrWhiteSpace(_animatorBool))
        {
            _animator.SetBool(_animatorBool, IsOn);
        }

        float angle = IsOn ? _onAngle : _offAngle;
        _targetLocalRotation = _startLocalRotation * Quaternion.Euler(GetAxisEuler(angle));
    }

    private void ApplyVisualInstantly()
    {
        if (_visualTarget != null)
        {
            _visualTarget.localRotation = _targetLocalRotation;
        }
    }

    private Vector3 GetAxisEuler(float angle)
    {
        switch (_rotationAxis)
        {
            case RotationAxis.Y:
                return new Vector3(0f, angle, 0f);
            case RotationAxis.Z:
                return new Vector3(0f, 0f, angle);
            default:
                return new Vector3(angle, 0f, 0f);
        }
    }
}
