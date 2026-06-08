using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    public InventoryItem item;

    public string Prompt => item != null ? $"Pick up {item.displayName}" : "Pick up";

    public void Interact()
    {
        if (item == null) { Destroy(gameObject); return; }
        if (PlayerInventory.Instance == null) return;

        if (PlayerInventory.Instance.AddItem(item.Clone()))
        {
            PickupFeedback.Show(item.displayName, item.kind == InventoryItem.ItemKind.Key);
            Destroy(gameObject);
        }
    }

    public static DroppedItem Spawn(InventoryItem item, Vector3 pos, Vector3 toss)
    {
        var go = new GameObject("Dropped_" + (item != null ? item.displayName : "Item"));
        go.transform.position = pos;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 1.5f;

        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.18f;

        var dropped = go.AddComponent<DroppedItem>();
        dropped.item = item != null ? item.Clone() : null;

        if (item != null && item.icon != null)
        {
            AddSprite(go.transform, "Icon", item.icon, item.iconTint.a <= 0f ? Color.white : item.iconTint, 0);
            if (item.iconOverlay != null) AddSprite(go.transform, "IconOverlay", item.iconOverlay, Color.white, 1);

            var lgo = new GameObject("Glow");
            lgo.transform.SetParent(go.transform, false);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            var tint = item.iconTint.a <= 0f ? new Color(1f, 0.9f, 0.7f) : item.iconTint;
            light.color = tint;
            light.intensity = 1.1f;
            light.range = 2.2f;
            light.shadows = LightShadows.None;
        }

        rb.AddForce(toss, ForceMode.Impulse);
        return dropped;
    }

    static void AddSprite(Transform parent, string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float s = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (s > 0.0001f) go.transform.localScale = Vector3.one * (0.3f / s);

        go.AddComponent<Billboard>();
    }
}

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
    }
}
