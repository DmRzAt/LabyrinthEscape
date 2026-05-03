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
    public static event System.Action<int, InventoryItem> OnHotbarSlotChanged; // (slotIndex, item)
    public static event System.Action<int, InventoryItem> OnActiveSlotChanged; // (slotIndex, item)

    void Awake()
    {
        Instance = this;
        if (hotbar == null || hotbar.Length != hotbarSize)
            hotbar = new InventoryItem[hotbarSize];
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

        // авто-додаємо у перший порожній слот hotbar
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbar[i] == null)
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
        // якщо предмет вже в іншому слоті — приберемо звідти
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
            bool hasSword = item != null && item.kind == InventoryItem.ItemKind.Sword;
            atk.hasSword = hasSword;
            if (atk.sword != null) atk.sword.gameObject.SetActive(hasSword);
        }
    }
}
