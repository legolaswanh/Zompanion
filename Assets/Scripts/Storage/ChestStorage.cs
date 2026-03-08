using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 宝箱储物数据，每个宝箱实例持有独立数据，支持存档。
/// </summary>
public class ChestStorage : IStorageData
{
    private readonly int _capacity;
    private readonly List<InventorySlot> _slots = new List<InventorySlot>();

    public List<InventorySlot> Slots => _slots;

    public int Capacity => _capacity;

    public event Action OnInventoryUpdated;

    public ChestStorage(int capacity = 20)
    {
        _capacity = Mathf.Max(1, capacity);
        Initialize();
    }

    public void Initialize()
    {
        _slots.Clear();
        for (int i = 0; i < _capacity; i++)
            _slots.Add(new InventorySlot(null));
    }

    public void SetItemAt(int index, ItemDataSO item)
    {
        if (index < 0 || index >= _slots.Count)
            return;

        _slots[index].itemData = item;
        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItem(ItemDataSO item)
    {
        if (item == null)
            return;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].itemData != item)
                continue;

            _slots[i].Clear();
            OnInventoryUpdated?.Invoke();
            return;
        }
    }

    public bool AddItem(ItemDataSO item)
    {
        InventorySlot emptySlot = _slots.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot == null)
        {
            OnInventoryUpdated?.Invoke();
            return false;
        }

        emptySlot.itemData = item;
        OnInventoryUpdated?.Invoke();
        return true;
    }
}
