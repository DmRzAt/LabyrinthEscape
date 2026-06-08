using System.Collections.Generic;
using UnityEngine;

public class EnemyZone : MonoBehaviour
{
    [Tooltip("Enemies of this zone. If left empty, all EnemyAI under this object are collected at Awake.")]
    public List<EnemyAI> enemies = new List<EnemyAI>();

    [Tooltip("If true, enemies start dormant (aiActive=false) and only wake when the player enters the zone trigger.")]
    public bool startDormant = true;

    public bool debugLogs = true;

    public bool IsActive { get; private set; }

    void Awake()
    {
        if (enemies == null || enemies.Count == 0)
            enemies = new List<EnemyAI>(GetComponentsInChildren<EnemyAI>(true));

        if (startDormant) SetActive(false);
        else SetActive(true);
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null) enemies[i].aiActive = active;
        }
        if (debugLogs)
            Debug.Log($"[EnemyZone] '{name}' -> {(active ? "ACTIVE" : "dormant")} ({enemies.Count} enemies)", this);
    }
}
