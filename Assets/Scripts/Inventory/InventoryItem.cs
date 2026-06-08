using System;
using UnityEngine;

[Serializable]
public class InventoryItem
{
	public enum ItemKind
	{
		Sword,
		Key,
		Potion,
		Generic,
		Note
	}

	public enum PotionEffect
	{
		None,
		Heal,
		Regen,
		Speed,
		Jump,
		Stamina
	}

	public string id;

	public string displayName;

	public ItemKind kind;

	public Sprite icon;

	public Color iconTint = Color.white;

	public Sprite iconOverlay;

	public int count = 1;

	public bool stackable;

	public float weight = 1f;

	[Header("Potion")]
	public PotionEffect potionEffect;

	public float potionMagnitude;

	public float potionDuration;

	[Header("Note")]
	[TextArea(2, 6)]
	public string noteText;

	public bool IsEmpty => string.IsNullOrEmpty(id);

	public InventoryItem Clone()
	{
		return new InventoryItem
		{
			id = id,
			displayName = displayName,
			kind = kind,
			icon = icon,
			iconTint = iconTint,
			iconOverlay = iconOverlay,
			count = count,
			stackable = stackable,
			weight = weight,
			potionEffect = potionEffect,
			potionMagnitude = potionMagnitude,
			potionDuration = potionDuration,
			noteText = noteText
		};
	}
}
