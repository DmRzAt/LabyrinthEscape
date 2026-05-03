using System;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    public enum ItemType { Key, Generic, Sword }

    [Serializable]
    public class ChestItem
    {
        public string name = "Item";
        public ItemType type = ItemType.Generic;
        public int count = 1;
    }

    [Header("Lid")]
    public Transform lid;
    public string lidChildName = "Chest_Top";
    public Vector3 lidOpenEuler = new Vector3(-90f, 0f, 0f);
    public float openSpeed = 3f;

    [Header("Items")]
    public List<ChestItem> items = new List<ChestItem> { new ChestItem { name = "Key", type = ItemType.Key, count = 1 } };

    public string Prompt => _opened ? "Look Inside" : "Open Chest";

    Quaternion _lidClosed;
    Quaternion _lidOpen;
    bool _opened;

    void Start()
    {
        if (lid == null) lid = FindLid(transform);
        if (lid != null)
        {
            _lidClosed = lid.localRotation;
            _lidOpen = _lidClosed * Quaternion.Euler(lidOpenEuler);
        }
    }

    Transform FindLid(Transform root)
    {
        foreach (Transform t in root)
        {
            if (t.name.Contains(lidChildName)) return t;
            var nested = FindLid(t);
            if (nested != null) return nested;
        }
        return null;
    }

    void Update()
    {
        if (lid == null) return;
        Quaternion target = _opened ? _lidOpen : _lidClosed;
        lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * openSpeed);
    }

    public void Interact()
    {
        _opened = true;
        ChestUI.Instance.Open(this);
    }

    public void TakeItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        var it = items[index];
        if (it.type == ItemType.Key && GameManager.Instance != null)
        {
            for (int i = 0; i < it.count; i++) GameManager.Instance.AddKey();
        }
        else if (it.type == ItemType.Sword)
        {
            Debug.Log("[Chest] Sword item taken, looking for PlayerAttack...");
            var atk = UnityEngine.Object.FindFirstObjectByType<PlayerAttack>(FindObjectsInactive.Include);
            if (atk != null)
            {
                Debug.Log("[Chest] PlayerAttack found on: " + atk.gameObject.name + ". Calling EquipSword.");
                atk.EquipSword();
            }
            else Debug.LogWarning("[Chest] PlayerAttack NOT found in scene. Add the component to the Player.");
        }
        items.RemoveAt(index);
    }

    public void TakeAll()
    {
        for (int i = items.Count - 1; i >= 0; i--) TakeItem(i);
    }
}
