using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight")]
    public Light flashlight;         // SpotLight на HandsRoot
    public KeyCode toggleKey = KeyCode.F;

    [Header("Flicker (імітація лампи)")]
    public bool enableFlicker = true;
    public float flickerSpeed = 0.05f;
    public float minIntensity = 1.5f;
    public float maxIntensity = 2.2f;

    private bool isOn = true;
    private float flickerTimer;

    void Start()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();

        flashlight.enabled = isOn;
    }

    void Update()
    {
        // Вмикання/вимикання клавішею F
        if (Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;
            flashlight.enabled = isOn;
        }

        // Мерехтіння як стара лампа
        if (isOn && enableFlicker)
            Flicker();
    }

    void Flicker()
    {
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            flashlight.intensity = Random.Range(minIntensity, maxIntensity);
            flickerTimer = flickerSpeed;
        }
    }
}