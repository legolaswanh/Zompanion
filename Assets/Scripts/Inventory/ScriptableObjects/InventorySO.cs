using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Inventory/Inventory System")]
public class InventorySO : ScriptableObject
{
    [Header("Config")]
    public int baseCapacity = 10;
    public int extraCapacity = 0;

    public int MaxCapacity => baseCapacity + extraCapacity;

    [Header("Debug")]
    [Tooltip("Clear all inventory slots at game start.")]
    public bool clearOnStart = true;

    [Header("Data")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    public event System.Action OnInventoryUpdated;

    public void Initialize()
    {
        if (slots.Count == MaxCapacity)
            return;

        slots.Clear();
        for (int i = 0; i < MaxCapacity; i++)
            slots.Add(new InventorySlot(null));
    }

    [ContextMenu("Clear Inventory")]
    public void ClearAll()
    {
        foreach (var slot in slots)
            slot.Clear();

        OnInventoryUpdated?.Invoke();
        Debug.Log("Inventory cleared.");
    }

    public bool AddItem(ItemDataSO item)
    {
        InventorySlot emptySlot = slots.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot == null)
        {
            OnInventoryUpdated?.Invoke();
            return false;
        }

        emptySlot.itemData = item;
        OnInventoryUpdated?.Invoke();
        return true;
    }

    public void SetItemAt(int index, ItemDataSO item)
    {
        if (index < 0 || index >= slots.Count)
            return;

        slots[index].itemData = item;
        OnInventoryUpdated?.Invoke();
    }

    public void ExpandCapacity(int amount)
    {
        if (amount == 0)
            return;

        if (amount > 0)
        {
            extraCapacity += amount;
            for (int i = 0; i < amount; i++)
                slots.Add(new InventorySlot(null));

            OnInventoryUpdated?.Invoke();
            return;
        }

        int targetExtra = Mathf.Max(0, extraCapacity + amount);
        int targetCount = baseCapacity + targetExtra;

        // Safety: only remove trailing empty slots.
        while (slots.Count > targetCount)
        {
            int last = slots.Count - 1;
            if (!slots[last].IsEmpty)
                break;

            slots.RemoveAt(last);
        }

        extraCapacity = Mathf.Max(0, slots.Count - baseCapacity);
        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItem(ItemDataSO item)
    {
        if (item == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].itemData != item)
                continue;

            slots[i].Clear();
            OnInventoryUpdated?.Invoke();
            return;
        }
    }
}
