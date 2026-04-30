using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [Range(0f, 1f)] public float flickerStrength = 0.4f;
    public float flickerSpeed = 9f;
    public float baseIntensity = 3.5f;

    Light _light;
    float _noiseOffset;

    void Awake()
    {
        _light = GetComponent<Light>();
        _noiseOffset = Random.Range(0f, 100f);
        if (_light != null) baseIntensity = _light.intensity;
    }

    void Update()
    {
        if (_light == null) return;
        float t = Time.time * flickerSpeed + _noiseOffset;
        float n = Mathf.PerlinNoise(t, 0f);
        _light.intensity = baseIntensity * (1f - flickerStrength * (1f - n));
    }
}
