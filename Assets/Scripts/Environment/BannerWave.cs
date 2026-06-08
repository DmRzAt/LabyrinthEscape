using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class BannerWave : MonoBehaviour
{
    public float amplitude = 0.14f;
    public float speed = 1.6f;
    public float waveCount = 1.4f;
    public float swaySide = 0.35f;
    public float activeDistance = 22f;

    [Header("Wind response")]
    [Tooltip("How much DungeonWind drives the motion. 0 = ignore wind (constant), 1 = full.")]
    public float windResponse = 1f;
    [Tooltip("Motion that stays even with no wind (so it never looks frozen).")]
    [Range(0f, 1f)] public float idleMotion = 0.3f;

    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] work;
    int lenAxis, normAxis, sideAxis;
    float minL, maxL;
    bool topIsMax;
    float outwardSign = 1f;
    Vector3 worldSide;
    float phase;
    bool ready;
    bool resting = true;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) { enabled = false; return; }

        mesh = Instantiate(mf.sharedMesh);
        mesh.MarkDynamic();
        mf.mesh = mesh;
        baseVerts = mesh.vertices;
        if (baseVerts == null || baseVerts.Length == 0) { enabled = false; return; }
        work = new Vector3[baseVerts.Length];

        Vector3 size = mesh.bounds.size;
        lenAxis = (size.x >= size.y && size.x >= size.z) ? 0 : (size.y >= size.z ? 1 : 2);
        normAxis = (size.x <= size.y && size.x <= size.z) ? 0 : (size.y <= size.z ? 1 : 2);
        sideAxis = 3 - lenAxis - normAxis;

        minL = float.MaxValue; maxL = float.MinValue;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            float l = baseVerts[i][lenAxis];
            if (l < minL) minL = l;
            if (l > maxL) maxL = l;
        }
        Vector3 a = baseVerts[0]; a[lenAxis] = maxL;
        Vector3 b = baseVerts[0]; b[lenAxis] = minL;
        topIsMax = transform.TransformPoint(a).y >= transform.TransformPoint(b).y;

        Vector3 ln = Vector3.zero; ln[normAxis] = 1f;
        Vector3 worldOut = transform.TransformDirection(ln).normalized;
        Vector3 c = mesh.bounds.center; c = transform.TransformPoint(c);
        float dPlus = SideClearance(c, worldOut);
        float dMinus = SideClearance(c, -worldOut);
        outwardSign = (dPlus >= dMinus) ? 1f : -1f;

        Vector3 ls = Vector3.zero; ls[sideAxis] = 1f;
        worldSide = transform.TransformDirection(ls).normalized;

        phase = Random.value * 10f;
        ready = true;
    }

    float SideClearance(Vector3 origin, Vector3 dir)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin + dir * 0.02f, dir, out hit, 1.0f, ~0, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return 1.0f;
    }

    void Update()
    {
        if (!ready) return;

        var cam = Camera.main;
        if (cam != null &&
            (cam.transform.position - transform.position).sqrMagnitude > activeDistance * activeDistance)
        {
            if (!resting)
            {
                mesh.vertices = baseVerts;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                resting = true;
            }
            return;
        }
        resting = false;

        float gust = 1f;
        float lean = 0f;
        var wind = DungeonWind.Instance;
        if (wind != null && windResponse > 0f)
        {
            float s = wind.Strength01;
            gust = Mathf.Lerp(1f, idleMotion + s, windResponse);
            lean = Mathf.Clamp(Vector3.Dot(wind.Direction, worldSide), -1f, 1f) * s * windResponse;
        }

        float span = Mathf.Max(0.0001f, maxL - minL);
        float t = Time.time * speed * Mathf.Max(0.4f, gust) + phase;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 v = baseVerts[i];
            float l = v[lenAxis];
            float d = topIsMax ? (maxL - l) / span : (l - minL) / span;
            float w = d * d;
            float bulge = 0.5f + 0.5f * Mathf.Sin(t + d * Mathf.PI * waveCount);
            v[normAxis] += outwardSign * amplitude * gust * w * bulge;
            v[sideAxis] += amplitude * w * (swaySide * gust * Mathf.Cos(t * 0.8f + d * Mathf.PI) + lean);
            work[i] = v;
        }
        mesh.vertices = work;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
