using UnityEngine;

[DisallowMultipleComponent]
public class DungeonWind : MonoBehaviour
{
	[Header("Base wind")]
	public Vector3 direction = new Vector3(1f, 0f, 0.35f);

	[Range(0f, 2f)]
	public float baseStrength = 0.45f;

	[Range(0f, 2f)]
	public float gustStrength = 0.6f;

	[Range(0.01f, 2f)]
	public float gustFrequency = 0.18f;

	[Range(0f, 1f)]
	public float directionWander = 0.25f;

	[Header("Particle WindZone (optional)")]
	public WindZone windZone;

	public float windZoneScale = 1.2f;

	private Vector3 dirNorm;

	public static DungeonWind Instance { get; private set; }

	public Vector3 Wind { get; private set; }

	public float Strength01 { get; private set; }

	public Vector3 Direction { get; private set; }

	private void Awake()
	{
		Instance = this;
		dirNorm = ((direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector3.right);
		Direction = dirNorm;
		if (windZone == null)
		{
			windZone = GetComponent<WindZone>();
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Update()
	{
		float time = Time.time;
		float num = Mathf.PerlinNoise(time * gustFrequency, 0.123f);
		num *= num;
		Strength01 = baseStrength + gustStrength * num;
		float num2 = (Mathf.PerlinNoise(0.77f, time * gustFrequency * 0.6f) - 0.5f) * 2f * directionWander;
		Direction = Quaternion.AngleAxis(num2 * 45f, Vector3.up) * dirNorm;
		Wind = Direction * Strength01;
		if (windZone != null)
		{
			windZone.mode = WindZoneMode.Directional;
			windZone.transform.rotation = Quaternion.LookRotation(Direction, Vector3.up);
			windZone.windMain = Strength01 * windZoneScale;
			windZone.windTurbulence = 0.3f + num * 0.9f;
			windZone.windPulseMagnitude = 0.5f;
			windZone.windPulseFrequency = 0.25f;
		}
	}
}
