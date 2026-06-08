using UnityEngine;

public class CameraShake : MonoBehaviour
{
	[Range(0f, 1f)]
	public float strength = 1f;

	[Tooltip("Perlin shake speed — higher = faster, jitterier")]
	public float shakeFrequency = 22f;

	private float _duration;

	private float _magnitude;

	private float _timer;

	private float _seedX;

	private float _seedY;

	private Vector3 _kickTarget;

	private Vector3 _kickVel;

	public static CameraShake Instance { get; private set; }

	public Vector3 CurrentOffset { get; private set; }

	public Vector3 CurrentRotationKick { get; private set; }

	private void Awake()
	{
		Instance = this;
		strength = PlayerPrefs.GetFloat("opt_shakeStrength", strength);
		_seedX = Random.value * 100f;
		_seedY = Random.value * 100f;
	}

	public static void Punch(Vector3 eulerKick)
	{
		if (Instance != null)
		{
			Instance._kickTarget += eulerKick * Instance.strength;
		}
	}

	public void SetStrength(float value)
	{
		strength = Mathf.Clamp01(value);
		PlayerPrefs.SetFloat("opt_shakeStrength", strength);
	}

	public static void Shake(float duration, float magnitude)
	{
		if (Instance != null)
		{
			Instance.StartShake(duration, magnitude);
		}
	}

	private void StartShake(float duration, float magnitude)
	{
		float num = ((_timer > 0f && _duration > 0f) ? (_magnitude * (_timer / _duration)) : 0f);
		if (magnitude >= num)
		{
			_duration = duration;
			_magnitude = magnitude;
			_timer = duration;
		}
		else
		{
			_timer = Mathf.Max(_timer, duration);
		}
	}

	private void LateUpdate()
	{
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		if (_timer > 0f)
		{
			_timer -= unscaledDeltaTime;
			float num = ((_duration > 0f) ? (_timer / _duration) : 0f);
			float num2 = _magnitude * num * strength;
			float y = Time.unscaledTime * shakeFrequency;
			float x = (Mathf.PerlinNoise(_seedX, y) - 0.5f) * 2f;
			float y2 = (Mathf.PerlinNoise(_seedY, y) - 0.5f) * 2f;
			CurrentOffset = new Vector3(x, y2, 0f) * num2;
		}
		else
		{
			CurrentOffset = Vector3.Lerp(CurrentOffset, Vector3.zero, unscaledDeltaTime * 10f);
		}
		_kickTarget = Vector3.Lerp(_kickTarget, Vector3.zero, unscaledDeltaTime * 14f);
		CurrentRotationKick = Vector3.SmoothDamp(CurrentRotationKick, _kickTarget, ref _kickVel, 0.05f, float.PositiveInfinity, unscaledDeltaTime);
	}
}
