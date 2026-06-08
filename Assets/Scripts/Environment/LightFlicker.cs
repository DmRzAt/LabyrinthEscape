using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float intensityAmount = 0.9f;
    public float rangeAmount = 0.4f;
    public float speed = 7f;

    private Light _light;
    private float _baseIntensity;
    private float _baseRange;
    private float _seed;

    void Start()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _baseRange = _light.range;
        _seed = Random.value * 100f;
    }

    void Update()
    {
        if (_light == null) return;
        float n = Mathf.PerlinNoise(_seed, Time.time * speed) - 0.5f;
        _light.intensity = Mathf.Max(0f, _baseIntensity + n * 2f * intensityAmount);
        _light.range = Mathf.Max(0.1f, _baseRange + n * 2f * rangeAmount);
    }
}
