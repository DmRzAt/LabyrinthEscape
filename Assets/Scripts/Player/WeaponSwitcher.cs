using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string itemName;
        public GameObject model;
    }

    [SerializeField] private Entry[] weapons;

    void OnEnable()
    {
        PlayerInventory.OnActiveSlotChanged += OnActiveSlot;
        PlayerInventory.OnHotbarSlotChanged += OnHotbarSlot;
    }

    void OnDisable()
    {
        PlayerInventory.OnActiveSlotChanged -= OnActiveSlot;
        PlayerInventory.OnHotbarSlotChanged -= OnHotbarSlot;
    }

    void Start()
    {
        HideAll();
        Refresh();
    }

    void OnActiveSlot(int slot, InventoryItem item) => Show(item);
    void OnHotbarSlot(int slot, InventoryItem item) => Refresh();

    void Refresh()
    {
        var inv = PlayerInventory.Instance;
        InventoryItem item = null;
        if (inv != null && inv.activeSlot >= 0 && inv.activeSlot < inv.hotbarSize)
            item = inv.hotbar[inv.activeSlot];
        Show(item);
    }

    void Show(InventoryItem item)
    {
        HideAll();
        if (item == null || item.IsEmpty || item.kind != InventoryItem.ItemKind.Sword) return;
        var e = Match(item);
        if (e != null && e.model != null) e.model.SetActive(true);
    }

    Entry Match(InventoryItem item)
    {
        if (weapons == null) return null;
        foreach (var e in weapons)
        {
            if (e == null || string.IsNullOrEmpty(e.itemName)) continue;
            if (item.displayName == e.itemName ||
                (!string.IsNullOrEmpty(item.id) && item.id.StartsWith(e.itemName + "_")))
                return e;
        }
        return null;
    }

    void HideAll()
    {
        if (weapons == null) return;
        foreach (var e in weapons)
            if (e != null && e.model != null) e.model.SetActive(false);
    }
}
