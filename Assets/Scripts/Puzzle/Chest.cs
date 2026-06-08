using System;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
	public enum ItemType
	{
		Key,
		Generic,
		Sword,
		Potion,
		Note
	}

	[Serializable]
	public class ChestItem
	{
		public string name = "Item";

		public ItemType type = ItemType.Generic;

		public int count = 1;

		public Sprite icon;

		public Color iconTint = Color.white;

		public Sprite iconOverlay;

		[Header("Potion (type == Potion)")]
		public InventoryItem.PotionEffect potionEffect;

		public float potionMagnitude;

		public float potionDuration;

		[Header("Note (type == Note)")]
		[TextArea(2, 6)]
		public string noteText = "Lever order:\n2  -  4  -  1  -  3";
	}

	[Header("Lid")]
	public Transform lid;

	public string lidChildName = "Chest_Top";

	public Vector3 lidOpenEuler = new Vector3(-90f, 0f, 0f);

	public float openSpeed = 3f;

	[Header("Items")]
	public List<ChestItem> items = new List<ChestItem>
	{
		new ChestItem
		{
			name = "Key",
			type = ItemType.Key,
			count = 1
		}
	};

	private Quaternion _lidClosed;

	private Quaternion _lidOpen;

	private bool _opened;

	public string Prompt
	{
		get
		{
			if (!_opened)
			{
				return "Open Chest";
			}
			return "Look Inside";
		}
	}

	private void Start()
	{
		if (lid == null)
		{
			lid = FindLid(base.transform);
		}
		if (lid != null)
		{
			_lidClosed = lid.localRotation;
			_lidOpen = _lidClosed * Quaternion.Euler(lidOpenEuler);
		}
	}

	private Transform FindLid(Transform root)
	{
		foreach (Transform item in root)
		{
			if (item.name.Contains(lidChildName))
			{
				return item;
			}
			Transform transform2 = FindLid(item);
			if (transform2 != null)
			{
				return transform2;
			}
		}
		return null;
	}

	private void Update()
	{
		if (!(lid == null))
		{
			Quaternion b = (_opened ? _lidOpen : _lidClosed);
			lid.localRotation = Quaternion.Slerp(lid.localRotation, b, Time.deltaTime * openSpeed);
		}
	}

	public void Interact()
	{
		_opened = true;
		if (ChestUI.Instance != null)
		{
			ChestUI.Instance.Open(this);
		}
		else
		{
			Debug.LogWarning("[Chest] ChestUI.Instance is null. Add ChestUI to the scene.", this);
		}
	}

	public void TakeItem(int index)
	{
		if (index < 0 || index >= items.Count)
		{
			return;
		}
		ChestItem chestItem = items[index];
		bool flag;
		if (chestItem.type == ItemType.Key && GameManager.Instance != null)
		{
			for (int i = 0; i < chestItem.count; i++)
			{
				GameManager.Instance.AddKey();
			}
			flag = true;
		}
		else if (PlayerInventory.Instance != null && chestItem.type != 0)
		{
			InventoryItem.ItemKind kind = ((chestItem.type != ItemType.Sword) ? ((chestItem.type == ItemType.Potion) ? InventoryItem.ItemKind.Potion : ((chestItem.type == ItemType.Note) ? InventoryItem.ItemKind.Note : InventoryItem.ItemKind.Generic)) : InventoryItem.ItemKind.Sword);
			InventoryItem item = new InventoryItem
			{
				id = chestItem.name + "_" + chestItem.type,
				displayName = chestItem.name,
				kind = kind,
				icon = chestItem.icon,
				iconTint = ((chestItem.iconTint.a <= 0f) ? Color.white : chestItem.iconTint),
				iconOverlay = chestItem.iconOverlay,
				count = chestItem.count,
				stackable = (chestItem.type == ItemType.Potion),
				weight = ((chestItem.type == ItemType.Note) ? 0f : 1f),
				potionEffect = chestItem.potionEffect,
				potionMagnitude = chestItem.potionMagnitude,
				potionDuration = chestItem.potionDuration,
				noteText = chestItem.noteText
			};
			flag = PlayerInventory.Instance.AddItem(item);
		}
		else if (chestItem.type == ItemType.Sword)
		{
			SwordCombat swordCombat = UnityEngine.Object.FindFirstObjectByType<SwordCombat>(FindObjectsInactive.Include);
			if (swordCombat != null)
			{
				swordCombat.SetEquipped(on: true);
			}
			flag = true;
		}
		else
		{
			flag = false;
		}
		if (flag)
		{
			PickupFeedback.Show(chestItem.name, chestItem.type == ItemType.Key);
			items.RemoveAt(index);
		}
	}

	public void TakeAll()
	{
		for (int num = items.Count - 1; num >= 0; num--)
		{
			TakeItem(num);
		}
	}
}
