using System.Collections;
using UnityEngine;

public class RewardReveal : MonoBehaviour
{
    [SerializeField] private float scaleInTime = 0.35f;
    [SerializeField] private float glowTime = 1.6f;
    [SerializeField] private Color glowColor = new Color(1f, 0.72f, 0.28f);
    [SerializeField] private float glowIntensity = 5f;
    [SerializeField] private float glowRange = 6f;
    [SerializeField] private float glowHeight = 1f;

    static AudioClip s_shimmer;
    Vector3 _fullScale = Vector3.one;
    bool _captured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_shimmer = null;

    void Awake()
    {
        _fullScale = transform.localScale;
        _captured = true;
    }

    public void Reveal()
    {
        if (!_captured) { _fullScale = transform.localScale; _captured = true; }

        if (s_shimmer == null) s_shimmer = ProceduralSfx.Chime(784f, 1568f, 0.5f, 0.7f);
        if (s_shimmer != null) AudioSource.PlayClipAtPoint(s_shimmer, transform.position, 1f);

        var chest = GetComponent<Chest>();
        if (chest != null) MazeMap.RegisterChestMarker(chest);

        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        var lgo = new GameObject("RevealGlow");
        lgo.transform.SetParent(transform, false);
        lgo.transform.localPosition = Vector3.up * glowHeight;
        var light = lgo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = glowRange;
        light.shadows = LightShadows.None;
        light.intensity = glowIntensity;

        transform.localScale = _fullScale * 0.2f;

        float dur = Mathf.Max(scaleInTime, glowTime);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = scaleInTime > 0f ? Mathf.Clamp01(t / scaleInTime) : 1f;
            float ease = 1f - Mathf.Pow(1f - s, 3f);
            transform.localScale = _fullScale * Mathf.Lerp(0.2f, 1f, ease);

            float g = glowTime > 0f ? 1f - Mathf.Clamp01(t / glowTime) : 0f;
            light.intensity = glowIntensity * g;
            yield return null;
        }

        transform.localScale = _fullScale;
        Destroy(lgo);
    }
}
