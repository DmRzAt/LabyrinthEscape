using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;        // Зменшено для кращого контролю в хорорі
    public float groundDrag = 6f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder;
    private float xRotation = 0f;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.3f;
    private float playerHeight = 2f;
    private bool isGrounded;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Вимикаємо гравітацію для ручного контролю або налаштовуємо Rigidbody
        rb.freezeRotation = true;

        // Ховаємо курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Перевірка приземлення
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - playerHeight / 2, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundMask);

        MyInput();
        Look();
        SpeedControl();

        // Налаштування тертя
        if (isGrounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);
    }

    private void MovePlayer()
    {
        // Напрямок руху відносно погляду гравця
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (isGrounded)
        {
            // Пряме керування швидкістю для усунення надмірної розгонистості
            Vector3 targetVelocity = moveDirection.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            // В повітрі додаємо трохи сили, щоб рух був менш різким
            rb.AddForce(moveDirection.normalized * moveSpeed * 2f, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        // Обмеження швидкості, щоб не було "ривків"
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }
}