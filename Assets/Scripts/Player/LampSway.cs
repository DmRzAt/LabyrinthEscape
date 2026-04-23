using UnityEngine;

public class LampSway : MonoBehaviour
{
    [Header("Bob - хитання при ходьбі")]
    public float walkBobSpeed = 4f;
    public float walkBobAmountX = 0.04f;
    public float walkBobAmountY = 0.03f;

    [Header("Sway - відхилення від миші")]
    public float swayAmount = 0.05f;
    public float swayMaxAmount = 0.1f;
    public float swaySmooth = 4f;

    [Header("Rotation Sway - поворот лампи")]
    public float rotSwayAmount = 8f;      // градуси
    public float rotSwayMaxAmount = 15f;
    public float rotSwaySmooth = 3f;

    [Header("Idle - дихання в спокої")]
    public float idleSpeed = 1.2f;
    public float idleAmount = 0.008f;

    [Header("Lag - затримка (вага лампи)")]
    public float positionLag = 6f;        // менше = важча лампа
    public float rotationLag = 4f;

    private Rigidbody playerRb;
    private Vector3 originPos;
    private Quaternion originRot;
    private float bobTimer;
    private float idleTimer;

    // Плавні значення
    private Vector3 swayPos;
    private Vector3 swayPosVelocity;
    private Quaternion swayRot;

    void Start()
    {
        playerRb = GetComponentInParent<Rigidbody>();
        originPos = transform.localPosition;
        originRot = transform.localRotation;
        swayRot = originRot;
    }

    void Update()
    {
        Vector3 flatVel = playerRb != null
            ? new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z)
            : Vector3.zero;

        bool isMoving = flatVel.magnitude > 0.2f;

        // Обчислення цільової позиції/повороту
        Vector3 targetPos = originPos + GetBob(isMoving) + GetIdle(isMoving) + GetMouseSway();
        Quaternion targetRot = originRot * GetMouseRotationSway() * GetWalkRoll(isMoving);

        // Плавне переміщення з затримкою (ефект ваги)
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPos,
            ref swayPosVelocity,
            1f / positionLag
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            Time.deltaTime * rotationLag
        );
    }

    // Хитання при ходьбі (вгору-вниз і в сторони)
    Vector3 GetBob(bool isMoving)
    {
        if (!isMoving)
        {
            bobTimer = Mathf.Lerp(bobTimer, 0, Time.deltaTime * 3f);
            return Vector3.zero;
        }

        bobTimer += Time.deltaTime * walkBobSpeed;
        float x = Mathf.Sin(bobTimer) * walkBobAmountX;
        float y = Mathf.Abs(Mathf.Cos(bobTimer)) * walkBobAmountY;
        return new Vector3(x, -y, 0);
    }

    // Дихання коли стоїш
    Vector3 GetIdle(bool isMoving)
    {
        if (isMoving) return Vector3.zero;

        idleTimer += Time.deltaTime * idleSpeed;
        float y = Mathf.Sin(idleTimer) * idleAmount;
        float x = Mathf.Sin(idleTimer * 0.7f) * idleAmount * 0.4f;
        return new Vector3(x, y, 0);
    }

    // Відхилення позиції від руху миші
    Vector3 GetMouseSway()
    {
        float mX = Input.GetAxisRaw("Mouse X");
        float mY = Input.GetAxisRaw("Mouse Y");

        float x = Mathf.Clamp(-mX * swayAmount, -swayMaxAmount, swayMaxAmount);
        float y = Mathf.Clamp(-mY * swayAmount, -swayMaxAmount, swayMaxAmount);
        return new Vector3(x, y, 0);
    }

    // Поворот лампи від руху миші (головна атмосферна фіча!)
    Quaternion GetMouseRotationSway()
    {
        float mX = Input.GetAxisRaw("Mouse X");
        float mY = Input.GetAxisRaw("Mouse Y");

        float rotX = Mathf.Clamp(mY * rotSwayAmount, -rotSwayMaxAmount, rotSwayMaxAmount);
        float rotY = Mathf.Clamp(-mX * rotSwayAmount, -rotSwayMaxAmount, rotSwayMaxAmount);
        float rotZ = Mathf.Clamp(mX * rotSwayAmount * 0.5f, -rotSwayMaxAmount, rotSwayMaxAmount);

        return Quaternion.Euler(rotX, rotY, rotZ);
    }

    // Легкий нахил вперед-назад при ходьбі
    Quaternion GetWalkRoll(bool isMoving)
    {
        if (!isMoving) return Quaternion.identity;
        float roll = Mathf.Sin(bobTimer) * 2f;
        return Quaternion.Euler(0, 0, roll);
    }
}