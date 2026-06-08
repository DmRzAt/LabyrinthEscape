using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
	[Header("Inventory")]
	public int maxSlots = 16;

	public List<InventoryItem> items = new List<InventoryItem>();

	[Header("Hotbar")]
	public int hotbarSize = 2;

	public InventoryItem[] hotbar;

	public int activeSlot = -1;

	public static PlayerInventory Instance { get; private set; }

	public float TotalWeight
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i] != null && !items[i].IsEmpty)
				{
					num += items[i].weight * (float)Mathf.Max(1, items[i].count);
				}
			}
			return num;
		}
	}

	public static event Action OnInventoryChanged;

	public static event Action<int, InventoryItem> OnHotbarSlotChanged;

	public static event Action<int, InventoryItem> OnActiveSlotChanged;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogWarning("[PlayerInventory] Duplicate instance detected, destroying.", this);
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			Instance = this;
			ResizeHotbar();
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		Instance = null;
	}

	private void OnValidate()
	{
		ResizeHotbar();
	}

	private void ResizeHotbar()
	{
		if (hotbarSize < 1)
		{
			hotbarSize = 1;
		}
		if (hotbar != null && hotbar.Length == hotbarSize)
		{
			return;
		}
		InventoryItem[] array = new InventoryItem[hotbarSize];
		if (hotbar != null)
		{
			int num = Mathf.Min(hotbar.Length, hotbarSize);
			for (int i = 0; i < num; i++)
			{
				array[i] = hotbar[i];
			}
		}
		hotbar = array;
	}

	private void Update()
	{
		if (GameManager.Instance != null && GameManager.Instance.IsPaused)
		{
			return;
		}
		Keyboard current = Keyboard.current;
		if (current == null)
		{
			return;
		}
		for (int i = 0; i < hotbarSize && i < 9; i++)
		{
			if (current[(Key)(41 + i)].wasPressedThisFrame)
			{
				SetActiveSlot(i);
			}
		}
		Mouse current2 = Mouse.current;
		if (current2 != null)
		{
			float y = current2.scroll.ReadValue().y;
			if (y < -0.01f)
			{
				CycleHotbar(1);
			}
			else if (y > 0.01f)
			{
				CycleHotbar(-1);
			}
		}
		if (current.qKey.wasPressedThisFrame)
		{
			UsePotion();
		}
	}

	private void CycleHotbar(int dir)
	{
		if (hotbar == null || hotbarSize <= 0)
		{
			return;
		}
		int num = ((activeSlot >= 0) ? activeSlot : ((dir > 0) ? (-1) : 0));
		for (int i = 0; i < hotbarSize; i++)
		{
			num = (num + dir + hotbarSize) % hotbarSize;
			InventoryItem inventoryItem = hotbar[num];
			if (inventoryItem != null && !inventoryItem.IsEmpty)
			{
				SetActiveSlot(num);
				break;
			}
		}
	}

	public void UsePotion()
	{
		InventoryItem inventoryItem = PickPotion();
		if (inventoryItem != null && TryApplyPotion(inventoryItem))
		{
			inventoryItem.count--;
			if (inventoryItem.count <= 0)
			{
				DropItem(inventoryItem);
			}
			else
			{
				PlayerInventory.OnInventoryChanged?.Invoke();
			}
		}
	}

	private bool TryApplyPotion(InventoryItem potion)
	{
		PlayerHealth component = GetComponent<PlayerHealth>();
		PlayerStatusEffects component2 = GetComponent<PlayerStatusEffects>();
		switch (potion.potionEffect)
		{
		case InventoryItem.PotionEffect.Heal:
			if (component == null || component.currentHP >= component.maxHP)
			{
				return false;
			}
			component.Heal(Mathf.RoundToInt(potion.potionMagnitude));
			return true;
		case InventoryItem.PotionEffect.Regen:
			if (component2 == null)
			{
				return false;
			}
			component2.Apply(potion.id, 1f, potion.potionDuration, 1f, potion.potionMagnitude, potion.displayName, potion.iconTint);
			return true;
		case InventoryItem.PotionEffect.Speed:
			if (component2 == null)
			{
				return false;
			}
			component2.Apply(potion.id, potion.potionMagnitude, potion.potionDuration, 1f, 0f, potion.displayName, potion.iconTint);
			return true;
		case InventoryItem.PotionEffect.Jump:
			if (component2 == null)
			{
				return false;
			}
			component2.Apply(potion.id, 1f, potion.potionDuration, potion.potionMagnitude, 0f, potion.displayName, potion.iconTint);
			return true;
		case InventoryItem.PotionEffect.Stamina:
			if (component2 == null)
			{
				return false;
			}
			component2.Apply(potion.id, 1f, potion.potionDuration, 1f, 0f, potion.displayName, potion.iconTint, potion.potionMagnitude);
			return true;
		default:
			return false;
		}
	}

	private InventoryItem PickPotion()
	{
		if (activeSlot >= 0 && activeSlot < hotbarSize)
		{
			InventoryItem inventoryItem = hotbar[activeSlot];
			if (inventoryItem != null && !inventoryItem.IsEmpty && inventoryItem.kind == InventoryItem.ItemKind.Potion)
			{
				return inventoryItem;
			}
		}
		PlayerHealth component = GetComponent<PlayerHealth>();
		if (component != null && component.currentHP < component.maxHP)
		{
			InventoryItem inventoryItem2 = items.Find((InventoryItem x) => IsPotion(x) && x.potionEffect == InventoryItem.PotionEffect.Heal);
			if (inventoryItem2 != null)
			{
				return inventoryItem2;
			}
		}
		InventoryItem inventoryItem3 = items.Find((InventoryItem x) => IsPotion(x) && x.potionEffect != InventoryItem.PotionEffect.Heal);
		if (inventoryItem3 != null)
		{
			return inventoryItem3;
		}
		return items.Find(IsPotion);
		static bool IsPotion(InventoryItem x)
		{
			if (x != null && !x.IsEmpty)
			{
				return x.kind == InventoryItem.ItemKind.Potion;
			}
			return false;
		}
	}

	public bool AddItem(InventoryItem item)
	{
		if (item == null)
		{
			return false;
		}
		if (item.stackable)
		{
			InventoryItem inventoryItem = items.Find((InventoryItem x) => x.id == item.id);
			if (inventoryItem != null)
			{
				inventoryItem.count += item.count;
				PlayerInventory.OnInventoryChanged?.Invoke();
				return true;
			}
		}
		if (items.Count >= maxSlots)
		{
			Debug.LogWarning("Inventory full!");
			return false;
		}
		items.Add(item);
		int num = ((item.kind != 0) ? (-1) : 0);
		if (num >= 0 && num < hotbarSize && (hotbar[num] == null || hotbar[num].IsEmpty))
		{
			hotbar[num] = item;
			PlayerInventory.OnHotbarSlotChanged?.Invoke(num, item);
			if (activeSlot < 0)
			{
				SetActiveSlot(num);
			}
		}
		else
		{
			for (int i = 0; i < hotbarSize; i++)
			{
				if (hotbar[i] == null || hotbar[i].IsEmpty)
				{
					hotbar[i] = item;
					PlayerInventory.OnHotbarSlotChanged?.Invoke(i, item);
					if (activeSlot < 0)
					{
						SetActiveSlot(i);
					}
					break;
				}
			}
		}
		PlayerInventory.OnInventoryChanged?.Invoke();
		return true;
	}

	public void AssignToHotbar(InventoryItem item, int slot)
	{
		if (slot < 0 || slot >= hotbarSize)
		{
			return;
		}
		for (int i = 0; i < hotbarSize; i++)
		{
			if (hotbar[i] == item)
			{
				hotbar[i] = null;
				PlayerInventory.OnHotbarSlotChanged?.Invoke(i, null);
			}
		}
		hotbar[slot] = item;
		PlayerInventory.OnHotbarSlotChanged?.Invoke(slot, item);
		if (activeSlot == slot)
		{
			ApplyActiveItem();
		}
	}

	public void RemoveFromHotbar(InventoryItem item)
	{
		if (item == null)
		{
			return;
		}
		for (int i = 0; i < hotbarSize; i++)
		{
			if (hotbar[i] == item)
			{
				hotbar[i] = null;
				PlayerInventory.OnHotbarSlotChanged?.Invoke(i, null);
				if (activeSlot == i)
				{
					ApplyActiveItem();
				}
			}
		}
	}

	public void DropItem(InventoryItem item)
	{
		if (item != null)
		{
			RemoveFromHotbar(item);
			items.Remove(item);
			PlayerInventory.OnInventoryChanged?.Invoke();
		}
	}

	public void DropItemToWorld(InventoryItem item)
	{
		if (item != null)
		{
			Vector3 forward = base.transform.forward;
			Camera main = Camera.main;
			if (main != null)
			{
				forward = main.transform.forward;
				forward.y = 0f;
				forward.Normalize();
			}
			if (forward.sqrMagnitude < 0.001f)
			{
				forward = base.transform.forward;
			}
			Vector3 vector = base.transform.position + Vector3.up * 1.1f;
			float num = 0.8f;
			if (Physics.Raycast(vector + forward * 0.35f, forward, out var hitInfo, num, -1, QueryTriggerInteraction.Ignore))
			{
				num = Mathf.Max(0.1f, 0.35f + hitInfo.distance - 0.25f);
			}
			Vector3 pos = vector + forward * num;
			DroppedItem.Spawn(item, pos, forward * 0.6f + Vector3.up * 0.5f);
			DropItem(item);
		}
	}

	public void SetActiveSlot(int slot)
	{
		if (slot >= 0 && slot < hotbarSize)
		{
			activeSlot = slot;
			ApplyActiveItem();
			PlayerInventory.OnActiveSlotChanged?.Invoke(slot, hotbar[slot]);
		}
	}

	private void ApplyActiveItem()
	{
		InventoryItem inventoryItem = ((activeSlot >= 0 && activeSlot < hotbarSize) ? hotbar[activeSlot] : null);
		SwordCombat component = GetComponent<SwordCombat>();
		if (component != null)
		{
			bool flag = inventoryItem != null && !inventoryItem.IsEmpty && inventoryItem.kind == InventoryItem.ItemKind.Sword;
			component.SetEquipped(flag);
			if (flag)
			{
				component.SetWeapon(inventoryItem.displayName);
			}
		}
	}
}
