using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LightDistanceCuller : MonoBehaviour
{
    public float cullDistance = 22f;
    public float checkInterval = 0.25f;
    public string[] nameFilters = { "point light", "torch", "flame" };

    struct Entry { public Light light; public Transform tr; public bool on; }
    readonly List<Entry> _entries = new List<Entry>();
    float _timer;

    void Start()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Point && l.type != LightType.Spot) continue;
            string n = l.gameObject.name.ToLowerInvariant();
            bool match = nameFilters.Length == 0;
            foreach (var f in nameFilters) if (n.Contains(f)) { match = true; break; }
            if (!match) continue;
            _entries.Add(new Entry { light = l, tr = l.transform, on = l.enabled });
        }
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = checkInterval;

        var cam = Camera.main;
        if (cam == null) return;
        Vector3 cp = cam.transform.position;
        float sqr = cullDistance * cullDistance;

        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.light == null) continue;
            bool want = (e.tr.position - cp).sqrMagnitude <= sqr;
            if (want == e.on) continue;
            e.light.enabled = want;
            e.on = want;
            _entries[i] = e;
        }
    }
}
