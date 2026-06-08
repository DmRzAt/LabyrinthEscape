using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	public float walkSpeed = 4f;

	public float sprintMultiplier = 1.5f;

	[Header("Dynamic speed")]
	public float agilityModifier;

	public float minSpeed = 1f;

	[Header("Encumbrance thresholds (weight)")]
	public float lightMaxWeight = 10f;

	public float heavyMaxWeight = 20f;

	[Range(0f, 1f)]
	public float heavySpeedMult = 0.7f;

	[Range(0f, 1f)]
	public float heavyJumpMult = 0.8f;

	[Range(0f, 1f)]
	public float overloadedSpeedMult = 0.45f;

	[Range(0f, 1f)]
	public float overloadedJumpMult = 0.5f;

	public float acceleration = 14f;

	public float deceleration = 18f;

	public float airControlMultiplier = 0.3f;

	public float jumpForce = 6f;

	public float groundDrag = 6f;

	[Header("Jump assistance")]
	public float coyoteTime = 0.12f;

	public float jumpBuffer = 0.12f;

	[Header("Jump abilities")]
	public int maxJumps = 1;

	[Header("Dash ability")]
	public bool dashUnlocked;

	public int maxDashes = 1;

	public float dashSpeed = 16f;

	public float dashCooldown = 0.8f;

	public Key dashKey = Key.LeftCtrl;

	[Header("Sprint stamina")]
	public float sprintStaminaPerSecond = 12f;

	public float minStaminaToSprint = 5f;

	[Header("Action movement")]
	[Range(0f, 1f)]
	public float blockMoveMultiplier = 0.4f;

	[Header("Mouse Look")]
	public float cm360 = 40f;

	public int mouseDPI = 800;

	public float sensitivityMultiplier = 1f;

	public bool invertX;

	public bool invertY;

	[Range(0f, 89f)]
	public float maxLookUp = 80f;

	[Range(0f, 89f)]
	public float maxLookDown = 80f;

	[Range(0f, 0.2f)]
	public float lookSmoothTime;

	public Transform cameraHolder;

	[Header("Crouch")]
	public Key crouchKey = Key.C;

	public float crouchSpeed = 2f;

	public float standHeight = 2f;

	public float crouchHeight = 1f;

	public float crouchTransitionSpeed = 10f;

	[Header("Headbob")]
	public bool headbobEnabled = true;

	public float bobFrequency = 8f;

	public float bobAmplitude = 0.05f;

	public float bobSprintMultiplier = 1.4f;

	[Header("FOV kick")]
	public Camera viewCamera;

	public float baseFov = 60f;

	[Tooltip("Extra FOV added on top of base while sprinting (so it scales with the FOV slider)")]
	public float sprintFovBoost = 8f;

	public float fovLerpSpeed = 8f;

	[Header("Landing impact")]
	public float landDipPerSpeed = 0.012f;

	public float maxLandDip = 0.16f;

	public float landDipRecover = 0.12f;

	[Header("Camera lean")]
	public float leanAngle = 4f;

	public float leanSmooth = 9f;

	[Header("Gravity")]
	public float gravity = 20f;

	public float groundingForce = 10f;

	public float maxFallSpeed = 55f;

	[Header("Step climbing")]
	public float stepHeight = 0.35f;

	public float stepCheckDistance = 0.15f;

	public float stepSmooth = 4f;

	[Header("Ground Check")]
	public LayerMask groundMask = -1;

	public float groundCheckRadius = 0.3f;

	public float groundCheckOffset = 0.05f;

	[Range(0f, 89f)]
	public float maxSlopeAngle = 55f;

	[Header("Footsteps")]
	public AudioSource footstepSource;

	public AudioClip[] footstepClips;

	public float footstepVolume = 0.5f;

	public float stepStrideLength = 2.2f;

	public AudioClip jumpClip;

	public AudioClip landClip;

	[Header("Noise (enemy hearing)")]
	public float walkNoiseRadius = 2f;

	public float sprintNoiseRadius = 6f;

	[Header("Smooth collisions")]
	public float bodyFriction;

	private float _yawVel;

	private float _pitchVel;

	private float _smoothYaw;

	private float _smoothPitch;

	private Rigidbody rb;

	private CapsuleCollider capsule;

	private PlayerStamina stamina;

	private PlayerHealth health;

	private SwordCombat swordCombat;

	private PlayerStatusEffects status;

	private PlayerStats stats;

	private Vector3 _moveInput;

	private float _xRotation;

	private bool _isGrounded;

	private bool _isSprinting;

	private float _coyoteTimer;

	private float _jumpBufferTimer;

	private int _jumpsUsed;

	private int _dashesUsed;

	private float _dashCdTimer;

	private bool _isCrouching;

	private float _standCamY;

	private float _bobTimer;

	private float _bobPrevSin;

	private Vector3 _camHolderBasePos;

	private Vector3 _groundNormal = Vector3.up;

	private bool _cursorLocked = true;

	private float _landDip;

	private float _landDipVel;

	private float _airFallSpeed;

	private float _lean;

	private bool _prevGrounded;

	private float _strideDist;

	public bool IsGrounded => _isGrounded;

	public float CurrentStepStride => stepStrideLength * (_isSprinting ? bobSprintMultiplier : 1f) * (_isCrouching ? 1.4f : 1f);

	public static int RelockFrame { get; private set; } = -1;


	public float CurrentMoveSpeed
	{
		get
		{
			GetEncumbrance(out var speedMult, out var _);
			float num = ((status != null) ? status.SpeedMultiplier : 1f);
			float num2 = ((stats != null) ? stats.SpeedModifier : 0f);
			return Mathf.Max(minSpeed, (walkSpeed + agilityModifier + num2) * speedMult * num);
		}
	}

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		rb.useGravity = false;
		stamina = GetComponent<PlayerStamina>();
		capsule = GetComponent<CapsuleCollider>();
		health = GetComponent<PlayerHealth>();
		swordCombat = GetComponent<SwordCombat>();
		status = GetComponent<PlayerStatusEffects>();
		stats = GetComponent<PlayerStats>();
		ConfigureSmoothCollisions();
		ReloadOptionPrefs();
		LockCursor(locked: true);
		if (viewCamera == null && cameraHolder != null)
		{
			viewCamera = cameraHolder.GetComponentInChildren<Camera>();
		}
		if (viewCamera != null)
		{
			viewCamera.fieldOfView = baseFov;
		}
		if (cameraHolder != null)
		{
			_camHolderBasePos = cameraHolder.localPosition;
		}
		_standCamY = _camHolderBasePos.y;
		if (capsule != null)
		{
			standHeight = capsule.height;
		}
	}

	private void OnEnable()
	{
		GameSettings.FovChanged += OnFovSettingChanged;
		GameManager.OnPauseChanged += OnPauseChanged;
	}

	private void OnDisable()
	{
		GameSettings.FovChanged -= OnFovSettingChanged;
		GameManager.OnPauseChanged -= OnPauseChanged;
	}

	private void OnFovSettingChanged(float fov)
	{
		baseFov = fov;
	}

	private void OnPauseChanged(bool paused)
	{
		if (!paused)
		{
			ReloadOptionPrefs();
		}
	}

	private void ReloadOptionPrefs()
	{
		sensitivityMultiplier = PlayerPrefs.GetFloat("opt_sensitivity", sensitivityMultiplier);
		invertX = PlayerPrefs.GetInt("opt_invertX", invertX ? 1 : 0) == 1;
		invertY = PlayerPrefs.GetInt("opt_invertY", invertY ? 1 : 0) == 1;
		baseFov = PlayerPrefs.GetFloat("opt_fov", baseFov);
		headbobEnabled = PlayerPrefs.GetInt("opt_headbob", headbobEnabled ? 1 : 0) == 1;
		lookSmoothTime = PlayerPrefs.GetFloat("opt_lookSmooth", lookSmoothTime);
	}

	private void ConfigureSmoothCollisions()
	{
		if (!(capsule == null))
		{
			PhysicsMaterial material = new PhysicsMaterial("PlayerSlide")
			{
				dynamicFriction = bodyFriction,
				staticFriction = bodyFriction,
				frictionCombine = PhysicsMaterialCombine.Minimum,
				bounciness = 0f,
				bounceCombine = PhysicsMaterialCombine.Minimum
			};
			capsule.material = material;
		}
	}

	private void Update()
	{
		GroundCheck();
		if (!_isGrounded)
		{
			_airFallSpeed = Mathf.Min(_airFallSpeed, rb.linearVelocity.y);
		}
		if (_isGrounded && !_prevGrounded)
		{
			float num = Mathf.Clamp(0f - _airFallSpeed, 0f, 22f);
			_landDip = Mathf.Min(maxLandDip, num * landDipPerSpeed);
			if (num > 3.5f)
			{
				CameraShake.Shake(0.12f, Mathf.Min(0.12f, num * 0.008f));
			}
			if (num > 1.5f && footstepSource != null && landClip != null)
			{
				footstepSource.PlayOneShot(landClip, Mathf.Clamp(num / 12f, 0.2f, 1f));
			}
			_airFallSpeed = 0f;
		}
		_prevGrounded = _isGrounded;
		if (health != null && health.IsDead)
		{
			_moveInput = Vector3.zero;
			_isSprinting = false;
			return;
		}
		ReadInput();
		HandleCrouch();
		HandleFootsteps();
		rb.linearDamping = ((_isGrounded && _moveInput.sqrMagnitude < 0.01f) ? groundDrag : 0f);
		if (_coyoteTimer > 0f)
		{
			_coyoteTimer -= Time.deltaTime;
		}
		if (_jumpBufferTimer > 0f)
		{
			_jumpBufferTimer -= Time.deltaTime;
		}
		if (_dashCdTimer > 0f)
		{
			_dashCdTimer -= Time.deltaTime;
		}
		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && _cursorLocked)
		{
			_jumpBufferTimer = jumpBuffer;
		}
		bool flag = _coyoteTimer > 0f;
		if (_jumpBufferTimer > 0f && _jumpsUsed < maxJumps && (flag || _jumpsUsed > 0))
		{
			DoJump();
			_jumpsUsed++;
			_jumpBufferTimer = 0f;
			_coyoteTimer = 0f;
		}
		TryDash();
	}

	private void LateUpdate()
	{
		Look();
		FovKick();
		Headbob();
	}

	private void DoJump()
	{
		Vector3 linearVelocity = rb.linearVelocity;
		linearVelocity.y = 0f;
		rb.linearVelocity = linearVelocity;
		GetEncumbrance(out var _, out var jumpMult);
		float num = ((status != null) ? status.JumpMultiplier : 1f);
		rb.AddForce(Vector3.up * jumpForce * jumpMult * num, ForceMode.VelocityChange);
		if (footstepSource != null && jumpClip != null)
		{
			footstepSource.PlayOneShot(jumpClip, 0.5f);
		}
	}

	private void TryDash()
	{
		if (dashUnlocked && _cursorLocked && !(_dashCdTimer > 0f) && _dashesUsed < maxDashes && Keyboard.current != null && Keyboard.current[dashKey].wasPressedThisFrame)
		{
			Vector3 vector = ((_moveInput.sqrMagnitude > 0.01f) ? _moveInput : base.transform.forward);
			vector.y = 0f;
			vector.Normalize();
			GetEncumbrance(out var speedMult, out var _);
			float num = ((status != null) ? status.SpeedMultiplier : 1f);
			float num2 = dashSpeed * speedMult * num;
			Vector3 linearVelocity = rb.linearVelocity;
			rb.linearVelocity = new Vector3(vector.x * num2, linearVelocity.y, vector.z * num2);
			_dashesUsed++;
			_dashCdTimer = dashCooldown;
		}
	}

	private void FixedUpdate()
	{
		Move();
		StepClimb();
		ApplyGravity();
	}

	private void StepClimb()
	{
		if (!_isGrounded || capsule == null)
		{
			return;
		}
		Vector3 vector = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
		if (vector.sqrMagnitude < 0.04f)
		{
			return;
		}
		vector.Normalize();
		float num = base.transform.position.y - capsule.height * 0.5f;
		float num2 = capsule.radius + stepCheckDistance;
		Vector3[] array = new Vector3[3]
		{
			vector,
			Quaternion.AngleAxis(35f, Vector3.up) * vector,
			Quaternion.AngleAxis(-35f, Vector3.up) * vector
		};
		foreach (Vector3 direction in array)
		{
			Vector3 origin = new Vector3(base.transform.position.x, num + 0.05f, base.transform.position.z);
			Vector3 origin2 = new Vector3(base.transform.position.x, num + stepHeight, base.transform.position.z);
			if (Physics.Raycast(origin, direction, num2, groundMask, QueryTriggerInteraction.Ignore) && !Physics.Raycast(origin2, direction, num2 + 0.05f, groundMask, QueryTriggerInteraction.Ignore))
			{
				rb.MovePosition(rb.position + Vector3.up * stepSmooth * Time.fixedDeltaTime);
				break;
			}
		}
	}

	private void ApplyGravity()
	{
		if (_isGrounded && rb.linearVelocity.y <= 0.01f)
		{
			rb.AddForce(Vector3.down * groundingForce, ForceMode.Acceleration);
		}
		else
		{
			rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
		}
		if (rb.linearVelocity.y < 0f - maxFallSpeed)
		{
			rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f - maxFallSpeed, rb.linearVelocity.z);
		}
	}

	private void GroundCheck()
	{
		float num = ((capsule != null) ? (capsule.height * 0.5f) : 1f);
		float num2 = ((capsule != null) ? Mathf.Min(groundCheckRadius, capsule.radius * 0.95f) : groundCheckRadius);
		Vector3 vector = ((capsule != null) ? capsule.center : Vector3.zero);
		if (Physics.SphereCast(base.transform.position + vector + Vector3.up * (num2 - num + groundCheckOffset), num2, Vector3.down, out var hitInfo, groundCheckOffset * 2f + 0.05f, groundMask, QueryTriggerInteraction.Ignore) && Vector3.Angle(hitInfo.normal, Vector3.up) <= maxSlopeAngle)
		{
			_isGrounded = true;
			_groundNormal = hitInfo.normal;
			_coyoteTimer = coyoteTime;
			if (rb != null && rb.linearVelocity.y <= 0.1f)
			{
				_jumpsUsed = 0;
				_dashesUsed = 0;
			}
		}
		else
		{
			_isGrounded = false;
			_groundNormal = Vector3.up;
		}
	}

	private void ReadInput()
	{
		Keyboard current = Keyboard.current;
		if (!_cursorLocked || current == null || Cursor.lockState != CursorLockMode.Locked)
		{
			_moveInput = Vector3.zero;
			_isSprinting = false;
			return;
		}
		float num = ((current.dKey.isPressed || current.rightArrowKey.isPressed) ? 1f : 0f) - ((current.aKey.isPressed || current.leftArrowKey.isPressed) ? 1f : 0f);
		float num2 = ((current.wKey.isPressed || current.upArrowKey.isPressed) ? 1f : 0f) - ((current.sKey.isPressed || current.downArrowKey.isPressed) ? 1f : 0f);
		_moveInput = base.transform.forward * num2 + base.transform.right * num;
		if (_moveInput.sqrMagnitude > 1f)
		{
			_moveInput.Normalize();
		}
		bool isPressed = current[crouchKey].isPressed;
		_isCrouching = isPressed || (_isCrouching && !CanStandUp());
		bool flag = current.leftShiftKey.isPressed && _moveInput.sqrMagnitude > 0.01f && !_isCrouching;
		bool flag2 = stamina == null || stamina.HasAtLeast(minStaminaToSprint);
		_isSprinting = flag && flag2 && !IsActionRestricted();
		if (_isSprinting && stamina != null)
		{
			stamina.DrainContinuous(sprintStaminaPerSecond);
		}
	}

	private bool CanStandUp()
	{
		if (capsule == null)
		{
			return true;
		}
		float num = standHeight - capsule.height + 0.05f;
		if (num <= 0f)
		{
			return true;
		}
		RaycastHit hitInfo;
		return !Physics.SphereCast(base.transform.position + capsule.center + Vector3.up * (capsule.height * 0.5f), capsule.radius * 0.95f, Vector3.up, out hitInfo, num, groundMask, QueryTriggerInteraction.Ignore);
	}

	private void HandleCrouch()
	{
		if (!(capsule == null))
		{
			float target = (_isCrouching ? crouchHeight : standHeight);
			float num = Mathf.MoveTowards(capsule.height, target, crouchTransitionSpeed * Time.deltaTime);
			capsule.height = num;
			capsule.center = new Vector3(0f, (num - standHeight) * 0.5f, 0f);
			if (cameraHolder != null)
			{
				float y = _standCamY + (num - standHeight) * 0.5f;
				Vector3 camHolderBasePos = _camHolderBasePos;
				camHolderBasePos.y = y;
				_camHolderBasePos = camHolderBasePos;
			}
		}
	}

	private void Look()
	{
		int num;
		if (_cursorLocked && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
		{
			if (!(health == null))
			{
				num = ((!health.IsDead) ? 1 : 0);
				if (num == 0)
				{
					goto IL_0156;
				}
			}
			else
			{
				num = 1;
			}
			Vector2 vector = Mouse.current.delta.ReadValue();
			float num2 = Mathf.Max(1f, cm360 * (float)mouseDPI / 2.54f);
			float num3 = 360f / num2 * sensitivityMultiplier;
			float num4 = vector.x * num3 * (invertX ? (-1f) : 1f);
			float num5 = vector.y * num3 * (invertY ? (-1f) : 1f);
			if (lookSmoothTime > 0.0001f)
			{
				_smoothYaw = Mathf.SmoothDamp(_smoothYaw, num4, ref _yawVel, lookSmoothTime);
				_smoothPitch = Mathf.SmoothDamp(_smoothPitch, num5, ref _pitchVel, lookSmoothTime);
			}
			else
			{
				_smoothYaw = num4;
				_smoothPitch = num5;
			}
			_xRotation = Mathf.Clamp(_xRotation - _smoothPitch, 0f - maxLookUp, maxLookDown);
			base.transform.Rotate(Vector3.up * _smoothYaw);
		}
		else
		{
			num = 0;
		}
		goto IL_0156;
		IL_0156:
		float b = (0f - ((num != 0) ? Vector3.Dot(_moveInput, base.transform.right) : 0f)) * leanAngle * (_isSprinting ? 1.25f : 1f);
		_lean = Mathf.Lerp(_lean, b, 1f - Mathf.Exp((0f - Time.deltaTime) * leanSmooth));
		Vector3 vector2 = ((CameraShake.Instance != null) ? CameraShake.Instance.CurrentRotationKick : Vector3.zero);
		if (cameraHolder != null)
		{
			cameraHolder.localRotation = Quaternion.Euler(_xRotation + vector2.x, vector2.y, vector2.z + _lean);
		}
	}

	private bool IsActionRestricted()
	{
		if (swordCombat != null)
		{
			return swordCombat.IsBlocking;
		}
		return false;
	}

	private void GetEncumbrance(out float speedMult, out float jumpMult)
	{
		float num = ((PlayerInventory.Instance != null) ? PlayerInventory.Instance.TotalWeight : 0f);
		if (stats != null)
		{
			num += stats.armorWeight;
		}
		float num2 = ((stats != null) ? stats.CarryCapacity : lightMaxWeight);
		float num3 = num2 + (heavyMaxWeight - lightMaxWeight);
		if (num <= num2)
		{
			speedMult = 1f;
			jumpMult = 1f;
		}
		else if (num <= num3)
		{
			speedMult = heavySpeedMult;
			jumpMult = heavyJumpMult;
		}
		else
		{
			speedMult = overloadedSpeedMult;
			jumpMult = overloadedJumpMult;
		}
	}

	private void Move()
	{
		float num = (_isCrouching ? crouchSpeed : (CurrentMoveSpeed * (_isSprinting ? sprintMultiplier : 1f)));
		if (IsActionRestricted())
		{
			num *= blockMoveMultiplier;
		}
		Vector3 vector = (_isGrounded ? (Vector3.ProjectOnPlane(_moveInput, _groundNormal).normalized * _moveInput.magnitude) : _moveInput) * num;
		Vector3 vector2 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
		Vector3 vector3 = new Vector3(vector.x, 0f, vector.z);
		bool flag = _moveInput.sqrMagnitude > 0.01f;
		if (flag || _isGrounded)
		{
			Vector3 force = Vector3.ClampMagnitude(maxLength: (!flag) ? deceleration : (_isGrounded ? acceleration : (acceleration * airControlMultiplier)), vector: (vector3 - vector2) / Time.fixedDeltaTime);
			rb.AddForce(force, ForceMode.Acceleration);
		}
	}

	private void Headbob()
	{
		if (cameraHolder == null)
		{
			return;
		}
		Vector3 vector = ((CameraShake.Instance != null) ? CameraShake.Instance.CurrentOffset : Vector3.zero);
		_landDip = Mathf.SmoothDamp(_landDip, 0f, ref _landDipVel, landDipRecover);
		Vector3 vector2 = new Vector3(0f, _landDip, 0f);
		vector -= vector2;
		if (!headbobEnabled)
		{
			cameraHolder.localPosition = _camHolderBasePos + vector;
			return;
		}
		float magnitude = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
		if (_isGrounded && magnitude > 0.5f)
		{
			float num = Mathf.Clamp(magnitude / Mathf.Max(0.5f, walkSpeed), 0.5f, bobSprintMultiplier);
			float num2 = bobFrequency * num;
			float num3 = bobAmplitude * num;
			_bobTimer += Time.deltaTime * num2;
			float num4 = Mathf.Sin(_bobTimer);
			float y = num4 * num3;
			float x = Mathf.Cos(_bobTimer * 0.5f) * num3 * 0.5f;
			_bobPrevSin = num4;
			cameraHolder.localPosition = _camHolderBasePos + new Vector3(x, y, 0f) + vector;
		}
		else
		{
			_bobTimer = 0f;
			_bobPrevSin = 0f;
			cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, _camHolderBasePos + vector, Time.deltaTime * 8f);
		}
	}

	private void HandleFootsteps()
	{
		if (!_isGrounded || _moveInput.sqrMagnitude < 0.01f)
		{
			_strideDist = 0f;
			return;
		}
		float magnitude = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
		if (magnitude < 0.5f)
		{
			_strideDist = 0f;
			return;
		}
		_strideDist += magnitude * Time.deltaTime;
		float num = stepStrideLength * (_isSprinting ? bobSprintMultiplier : 1f) * (_isCrouching ? 1.4f : 1f);
		if (_strideDist >= num)
		{
			_strideDist = 0f;
			PlayFootstep();
		}
	}

	private void PlayFootstep()
	{
		float loudness = (_isCrouching ? (walkNoiseRadius * 0.4f) : (_isSprinting ? sprintNoiseRadius : walkNoiseRadius));
		EnemyAI.NotifyNoise(base.transform.position, loudness);
		if (!(footstepSource == null) && footstepClips != null && footstepClips.Length != 0)
		{
			AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
			footstepSource.PlayOneShot(clip, footstepVolume * (_isCrouching ? 0.5f : 1f));
		}
	}

	private void FovKick()
	{
		if (!(viewCamera == null))
		{
			float b = baseFov + (_isSprinting ? sprintFovBoost : 0f);
			viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, b, Time.deltaTime * fovLerpSpeed);
		}
	}

	private void LockCursor(bool locked)
	{
		_cursorLocked = locked;
		Cursor.lockState = (locked ? CursorLockMode.Locked : CursorLockMode.None);
		Cursor.visible = !locked;
	}
}
