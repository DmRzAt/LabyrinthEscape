using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 _baseLocalPos;
    private float _duration;
    private float _magnitude;
    private float _timer;

    void Awake()
    {
        Instance = this;
        _baseLocalPos = transform.localPosition;
    }

    public static void Shake(float duration, float magnitude)
    {
        if (Instance != null) Instance.StartShake(duration, magnitude);
    }

    void StartShake(float duration, float magnitude)
    {
        _duration = duration;
        _magnitude = magnitude;
        _timer = duration;
    }

    void LateUpdate()
    {
        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            float k = _timer / _duration;
            Vector3 offset = Random.insideUnitSphere * _magnitude * k;
            transform.localPosition = _baseLocalPos + offset;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPos, Time.deltaTime * 10f);
        }
    }
}
