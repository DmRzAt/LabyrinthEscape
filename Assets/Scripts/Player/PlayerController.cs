using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundDrag = 6f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder; // порожній GameObject з Camera всередині

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.3f;

    private Rigidbody rb;
    private float xRotation = 0f;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // щоб Rigidbody не перекидав гравця

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        GroundCheck();
        MouseLook();
        ApplyDrag();
    }

    void FixedUpdate()
    {
        Move();
    }

    void GroundCheck()
    {
        // Перевірка чи гравець стоїть на землі
        isGrounded = Physics.CheckSphere(
            transform.position - new Vector3(0, 0.9f, 0),
            groundCheckRadius,
            groundMask
        );
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Поворот камери вгору/вниз
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Поворот тіла гравця вліво/вправо
        transform.Rotate(Vector3.up * mouseX);
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 dir = transform.forward * v + transform.right * h;
        dir.Normalize();

        // Застосовуємо силу тільки якщо стоїмо на землі
        if (isGrounded)
        {
            rb.AddForce(dir * moveSpeed * 10f, ForceMode.Force);
        }

        // Обмеження максимальної швидкості
        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.magnitude > moveSpeed)
        {
            Vector3 capped = flat.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(capped.x, rb.linearVelocity.y, capped.z);
        }
    }

    void ApplyDrag()
    {
        rb.linearDamping = isGrounded ? groundDrag : 0.5f;
    }
}