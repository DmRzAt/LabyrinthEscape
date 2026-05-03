using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintMultiplier = 1.5f;
    public float acceleration = 14f;        // плавний розгін
    public float deceleration = 18f;        // плавне гальмування
    public float jumpForce = 6f;
    public float groundDrag = 6f;

    [Header("Sprint stamina")]
    public float sprintStaminaPerSecond = 12f;
    public float minStaminaToSprint = 5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float mouseSmoothing = 0.05f;    // 0 = миттєво, 0.1 = плавно
    public Transform cameraHolder;

    [Header("Headbob")]
    public bool headbobEnabled = true;
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.05f;
    public float bobSprintMultiplier = 1.4f;

    [Header("FOV kick")]
    public Camera viewCamera;
    public float baseFov = 60f;
    public float sprintFov = 70f;
    public float fovLerpSpeed = 8f;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.3f;
    private float playerHeight = 2f;

    private Rigidbody rb;
    private PlayerStamina stamina;
    private Vector3 _moveInput;
    private Vector3 _currentVelocity;       // згладжена швидкість
    private float _xRotation = 0f;
    private float _smoothMouseX, _smoothMouseY;
    private float _smoothMouseXVel, _smoothMouseYVel;
    private bool _isGrounded;
    private bool _isSprinting;
    private float _bobTimer;
    private Vector3 _camHolderBasePos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        stamina = GetComponent<PlayerStamina>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (viewCamera == null && cameraHolder != null)
            viewCamera = cameraHolder.GetComponentInChildren<Camera>();

        if (cameraHolder != null) _camHolderBasePos = cameraHolder.localPosition;
    }

    void Update()
    {
        Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - playerHeight / 2, transform.position.z);
        _isGrounded = Physics.CheckSphere(spherePos, groundCheckRadius, groundMask);

        ReadInput();
        Look();
        Headbob();
        FovKick();

        rb.linearDamping = _isGrounded ? groundDrag : 0;

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        Move();
    }

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _moveInput = (transform.forward * v + transform.right * h).normalized;

        bool wantSprint = Input.GetKey(KeyCode.LeftShift) && v > 0.1f;
        // якщо немає PlayerStamina — sprint безкоштовний
        bool hasEnoughStamina = stamina == null || stamina.HasAtLeast(minStaminaToSprint);
        _isSprinting = wantSprint && hasEnoughStamina;

        if (_isSprinting && _moveInput.sqrMagnitude > 0.01f && stamina != null)
            stamina.DrainContinuous(sprintStaminaPerSecond);
    }

    void Look()
    {
        float rawX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float rawY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        if (mouseSmoothing > 0f)
        {
            _smoothMouseX = Mathf.SmoothDamp(_smoothMouseX, rawX, ref _smoothMouseXVel, mouseSmoothing);
            _smoothMouseY = Mathf.SmoothDamp(_smoothMouseY, rawY, ref _smoothMouseYVel, mouseSmoothing);
        }
        else
        {
            _smoothMouseX = rawX;
            _smoothMouseY = rawY;
        }

        _xRotation -= _smoothMouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        if (cameraHolder != null) cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * _smoothMouseX);
    }

    void Move()
    {
        float targetSpeed = walkSpeed * (_isSprinting ? sprintMultiplier : 1f);
        Vector3 targetVel = _moveInput * targetSpeed;
        Vector3 currentFlat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float rate = (_moveInput.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        Vector3 newFlat = Vector3.MoveTowards(currentFlat, targetVel, rate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newFlat.x, rb.linearVelocity.y, newFlat.z);
    }

    void Headbob()
    {
        if (!headbobEnabled || cameraHolder == null) return;

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = flat.magnitude;

        if (_isGrounded && speed > 0.5f)
        {
            float freq = bobFrequency * (_isSprinting ? bobSprintMultiplier : 1f);
            float amp = bobAmplitude * (_isSprinting ? bobSprintMultiplier : 1f);
            _bobTimer += Time.deltaTime * freq;
            float bobY = Mathf.Sin(_bobTimer) * amp;
            float bobX = Mathf.Cos(_bobTimer * 0.5f) * amp * 0.5f;
            cameraHolder.localPosition = _camHolderBasePos + new Vector3(bobX, bobY, 0f);
        }
        else
        {
            _bobTimer = 0f;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, _camHolderBasePos, Time.deltaTime * 8f);
        }
    }

    void FovKick()
    {
        if (viewCamera == null) return;
        float target = _isSprinting ? sprintFov : baseFov;
        viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, target, Time.deltaTime * fovLerpSpeed);
    }
}
