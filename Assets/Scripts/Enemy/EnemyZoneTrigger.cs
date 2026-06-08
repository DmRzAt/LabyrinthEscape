using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EnemyZoneTrigger : MonoBehaviour
{
    [Tooltip("Optional zone of pre-placed (static) enemies to wake. Leave empty if this zone only uses spawners.")]
    public EnemyZone zone;

    [Tooltip("Runtime spawners belonging to this zone. On player entry their Spawn() is called. Set their Spawn On Start = false.")]
    public CorridorEnemySpawner[] spawners;

    public bool activateOnEnter = true;
    public bool deactivateOnExit = false;
    [Tooltip("Seconds to wait after the player enters before waking the enemies / spawning.")]
    public float activationDelay = 0f;
    public bool debugLogs = true;

    private bool _activated;

    void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = true;
    }

    void Start()
    {
        if (zone == null) zone = GetComponentInParent<EnemyZone>();
        var bc = GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = true;
        bool hasSpawners = spawners != null && spawners.Length > 0;
        if (zone == null && !hasSpawners && debugLogs)
            Debug.LogWarning($"[EnemyZoneTrigger] '{name}' controls neither an EnemyZone nor any spawners.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugLogs) Debug.Log("[EnemyZone] Trigger entered by: " + other.name, this);
        if (!activateOnEnter || _activated || !other.CompareTag(Tags.Player)) return;
        _activated = true;
        if (debugLogs) Debug.Log($"[{GetZoneName()}] Player entered zone", this);
        if (activationDelay > 0f) Invoke(nameof(Activate), activationDelay);
        else Activate();
    }

    void OnTriggerExit(Collider other)
    {
        if (!deactivateOnExit || zone == null || !other.CompareTag(Tags.Player)) return;
        _activated = false;
        zone.SetActive(false);
    }

    void Activate()
    {
        if (zone != null) zone.SetActive(true);
        if (spawners != null)
        {
            for (int i = 0; i < spawners.Length; i++)
                if (spawners[i] != null) spawners[i].Spawn();
        }
    }

    private string GetZoneName()
    {
        if (zone != null) return zone.name;
        if (transform.parent != null) return transform.parent.name;
        return name;
    }

    void OnDrawGizmos()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.12f);
        Gizmos.DrawCube(bc.center, bc.size);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.85f);
        Gizmos.DrawWireCube(bc.center, bc.size);
    }
}
