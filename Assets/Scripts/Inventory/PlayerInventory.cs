using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Inventory")]
    public int maxSlots = 16;
    public List<InventoryItem> items = new List<InventoryItem>();

    [Header("Hotbar")]
    public int hotbarSize = 2;
    public InventoryItem[] hotbar;
    public int activeSlot = -1;

    public static event System.Action OnInventoryChanged;
    public static event System.Action<int, InventoryItem> OnHotbarSlotChanged;
    public static event System.Action<int, InventoryItem> OnActiveSlotChanged;

    void Awake()
    {
        Instance = this;
        ResizeHotbar();
    }

    void OnValidate()
    {
        ResizeHotbar();
    }

    void ResizeHotbar()
    {
        if (hotbarSize < 1) hotbarSize = 1;
        if (hotbar == null || hotbar.Length != hotbarSize)
        {
            var newArr = new InventoryItem[hotbarSize];
            if (hotbar != null)
            {
                int copy = Mathf.Min(hotbar.Length, hotbarSize);
                for (int i = 0; i < copy; i++) newArr[i] = hotbar[i];
            }
            hotbar = newArr;
        }
    }

    void Update()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SetActiveSlot(i);
        }
    }

    public void AddItem(InventoryItem item)
    {
        if (item == null) return;

        if (item.stackable)
        {
            var existing = items.Find(x => x.id == item.id);
            if (existing != null)
            {
                existing.count += item.count;
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        if (items.Count >= maxSlots) { Debug.LogWarning("Inventory full!"); return; }
        items.Add(item);
        Debug.Log($"[Inventory] Added {item.displayName} ({item.kind}). Total items: {items.Count}");

        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbar[i] == null || hotbar[i].IsEmpty)
            {
                hotbar[i] = item;
                OnHotbarSlotChanged?.Invoke(i, item);
                if (activeSlot < 0) SetActiveSlot(i);
                break;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public void AssignToHotbar(InventoryItem item, int slot)
    {
        if (slot < 0 || slot >= hotbarSize) return;
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbar[i] == item) { hotbar[i] = null; OnHotbarSlotChanged?.Invoke(i, null); }
        }
        hotbar[slot] = item;
        OnHotbarSlotChanged?.Invoke(slot, item);
        if (activeSlot == slot) ApplyActiveItem();
    }

    public void SetActiveSlot(int slot)
    {
        if (slot < 0 || slot >= hotbarSize) return;
        activeSlot = slot;
        ApplyActiveItem();
        OnActiveSlotChanged?.Invoke(slot, hotbar[slot]);
    }

    void ApplyActiveItem()
    {
        var item = (activeSlot >= 0 && activeSlot < hotbarSize) ? hotbar[activeSlot] : null;

        var atk = GetComponent<PlayerAttack>();
        if (atk != null)
        {
            bool hasSword = item != null && !item.IsEmpty && item.kind == InventoryItem.ItemKind.Sword;
            atk.SetSwordEquipped(hasSword);
        }
    }
}
