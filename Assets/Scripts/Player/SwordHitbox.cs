using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [HideInInspector] public PlayerAttack owner;

    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.OnHitboxTrigger(other);
    }
}
