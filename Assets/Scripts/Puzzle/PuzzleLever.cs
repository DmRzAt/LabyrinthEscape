using System;
using UnityEngine;

public class PuzzleLever : MonoBehaviour, IInteractable
{
	private enum RotationAxis
	{
		X,
		Y,
		Z
	}

	[Header("State")]
	[SerializeField]
	private bool _startsEnabled;

	[SerializeField]
	private bool _startsOn;

	[SerializeField]
	private bool _canToggleOff = true;

	[SerializeField]
	private bool _disableLegacyAnButton = true;

	[SerializeField]
	private bool _debugLogs = true;

	[Header("Prompt")]
	[SerializeField]
	private string _disabledPrompt = "Inactive Lever";

	[SerializeField]
	private string _offPrompt = "Pull Lever";

	[SerializeField]
	private string _onPrompt = "Lever On";

	[Header("Visuals")]
	[SerializeField]
	private Transform _handle;

	[SerializeField]
	private bool _rotateSelfIfHandleMissing = true;

	[SerializeField]
	private RotationAxis _rotationAxis;

	[SerializeField]
	[Range(-90f, 90f)]
	private float _offAngle;

	[SerializeField]
	[Range(-90f, 90f)]
	private float _onAngle = 55f;

	[SerializeField]
	[Range(1f, 20f)]
	private float _animationSpeed = 8f;

	[SerializeField]
	private Animator _animator;

	[SerializeField]
	private string _animatorBool = "LeverUp";

	private bool _isEnabled;

	private Transform _visualTarget;

	private Quaternion _startLocalRotation;

	private Quaternion _targetLocalRotation;

	private AudioSource _audio;

	private static AudioClip s_pullClip;

	public bool IsOn { get; private set; }

	public string Prompt
	{
		get
		{
			if (_isEnabled)
			{
				if (!IsOn)
				{
					return _offPrompt;
				}
				return _onPrompt;
			}
			return _disabledPrompt;
		}
	}

	public event Action<PuzzleLever, bool> StateChanged;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		s_pullClip = null;
	}

	private void Awake()
	{
		if (_disableLegacyAnButton)
		{
			DisableLegacyAnButton();
		}
		_visualTarget = ((_handle != null) ? _handle : (_rotateSelfIfHandleMissing ? base.transform : null));
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
		_audio = GetComponent<AudioSource>();
		if (_audio == null)
		{
			_audio = base.gameObject.AddComponent<AudioSource>();
		}
		_audio.playOnAwake = false;
		_audio.spatialBlend = 1f;
		_audio.maxDistance = 16f;
		_audio.rolloffMode = AudioRolloffMode.Linear;
		if (s_pullClip == null)
		{
			s_pullClip = ProceduralSfx.Thud(8731);
		}
	}

	private void Update()
	{
		if (!(_visualTarget == null))
		{
			_visualTarget.localRotation = Quaternion.Slerp(_visualTarget.localRotation, _targetLocalRotation, Time.deltaTime * _animationSpeed);
		}
	}

	public void SetEnabled(bool isEnabled)
	{
		_isEnabled = isEnabled;
	}

	public void Interact()
	{
		if (_debugLogs)
		{
			Debug.Log($"[PuzzleLever] Interact: {base.name}, enabled={_isEnabled}, isOn={IsOn}", this);
		}
		if (_isEnabled && (!IsOn || _canToggleOff))
		{
			SetOn(!IsOn);
		}
	}

	public void SetOn(bool isOn, bool notify = true)
	{
		if (IsOn == isOn)
		{
			return;
		}
		IsOn = isOn;
		if (notify)
		{
			if (_audio != null && s_pullClip != null)
			{
				_audio.pitch = UnityEngine.Random.Range(0.92f, 1.06f);
				_audio.PlayOneShot(s_pullClip, 0.7f);
			}
			this.StateChanged?.Invoke(this, IsOn);
		}
		UpdateTargetRotation();
	}

	private void DisableLegacyAnButton()
	{
		MonoBehaviour[] componentsInChildren = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
		foreach (MonoBehaviour monoBehaviour in componentsInChildren)
		{
			if (monoBehaviour != null && monoBehaviour.GetType().Name == "AN_Button")
			{
				monoBehaviour.enabled = false;
			}
		}
	}

	private void UpdateTargetRotation()
	{
		if (_animator != null && !string.IsNullOrWhiteSpace(_animatorBool))
		{
			_animator.SetBool(_animatorBool, IsOn);
		}
		float angle = (IsOn ? _onAngle : _offAngle);
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
		return _rotationAxis switch
		{
			RotationAxis.Y => new Vector3(0f, angle, 0f), 
			RotationAxis.Z => new Vector3(0f, 0f, angle), 
			_ => new Vector3(angle, 0f, 0f), 
		};
	}
}
