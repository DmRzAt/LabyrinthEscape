using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public enum ItemKind { Sword, Key, Potion, Generic }

    public string id;
    public string displayName;
    public ItemKind kind;
    public Sprite icon;
    public int count = 1;
    public bool stackable = false;

    public bool IsEmpty => string.IsNullOrEmpty(id);

    public InventoryItem Clone() => new InventoryItem
    {
        id = id,
        displayName = displayName,
        kind = kind,
        icon = icon,
        count = count,
        stackable = stackable
    };
}
