using System.Collections.Generic;
using UnityEngine;

public class ParticleDistanceCuller : MonoBehaviour
{
    public float cullDistance = 24f;
    public float checkInterval = 0.25f;
    public string[] nameFilters = { "flame", "ember", "smoke" };

    struct Entry { public ParticleSystem ps; public Transform tr; public bool on; }
    readonly List<Entry> entries = new List<Entry>();
    float timer;

    void Start()
    {
        foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            string n = ps.gameObject.name.ToLowerInvariant();
            bool match = false;
            foreach (var f in nameFilters) if (n.Contains(f)) { match = true; break; }
            if (!match) continue;
            entries.Add(new Entry { ps = ps, tr = ps.transform, on = true });
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        var cam = Camera.main;
        if (cam == null) return;
        Vector3 cp = cam.transform.position;
        float sqr = cullDistance * cullDistance;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.ps == null) continue;
            bool want = (e.tr.position - cp).sqrMagnitude <= sqr;
            if (want == e.on) continue;
            var em = e.ps.emission;
            em.enabled = want;
            e.on = want;
            entries[i] = e;
        }
    }
}
