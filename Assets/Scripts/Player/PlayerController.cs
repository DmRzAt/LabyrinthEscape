using UnityEngine;

/// <summary>
/// Kontroler gracza FPS — ruch (WASD), kamera (mysz), sprint, interakcja.
/// Obsługuje CharacterController dla kolizji.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraHolder;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private CharacterController characterController;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;

    private Camera playerCamera;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = cameraHolder != null
            ? cameraHolder.GetComponentInChildren<Camera>()
            : GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Не рухатися, якщо гра не в стані Playing
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }

    // ─────────────── Камера ───────────────

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вертикальний поворот (камера)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        Transform camTransform = cameraHolder != null ? cameraHolder : transform;
        camTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Горизонтальний поворот (тіло гравця)
        transform.Rotate(Vector3.up * mouseX);
    }

    // ─────────────── Рух ───────────────

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // тримаємо на землі
        }

        // Введення WASD
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        characterController.Move(move * currentSpeed * Time.deltaTime);

        // Стрибок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Гравітація
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // ─────────────── Взаємодія з об'єктами ───────────────

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    Debug.Log($"[Player] Interacted with: {hit.collider.gameObject.name}");
                }
            }
        }
    }

    /// <summary>
    /// Zwraca pozycję gracza (używane np. przez EnemyAI).
    /// </summary>
    public Vector3 GetPosition() => transform.position;
}
