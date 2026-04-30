using UnityEngine;

public class PuzzleSwitch : MonoBehaviour
{
    [Header("Polaczony obiekt")]
    public GameObject[] targets;
    public bool toggleActive = true;

    private bool _activated = false;

    void OnTriggerEnter(Collider other)
    {
        if (_activated || !other.CompareTag("Player")) return;
        _activated = true;

        foreach (var t in targets)
            if (t != null)
                t.SetActive(toggleActive ? !t.activeSelf : true);
    }
}
