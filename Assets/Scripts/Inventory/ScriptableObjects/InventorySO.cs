using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Inventory/Inventory System")]
public class InventorySO : ScriptableObject
{
    [Header("Config")]
    public int baseCapacity = 10; // MVP 设定为 10
    public int extraCapacity = 0; // 由僵尸提供的额外空间

    // 当前总容量
    public int MaxCapacity => baseCapacity + extraCapacity;

    [Header("调试")]
    [Tooltip("游戏开始时是否自动清空背包？")]
    public bool clearOnStart = true; // 勾选则自动清空，方便测试

    // 清空所有格子的方法
    [ContextMenu("Clear Inventory")]
    public void ClearAll()
    {
        foreach (var slot in slots)
        {
            // 调用单个格子的 Clear 方法（将 item 设为 null，数量设为 0）
            slot.Clear();
        }
        
        // 通知 UI 刷新
        OnInventoryUpdated?.Invoke();
        
        Debug.Log("背包已重置为空。");
    }

    [Header("Data")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    // 事件：当背包发生变化时通知 UI 更新
    public event System.Action OnInventoryUpdated;

    // 初始化（通常在游戏开始时重置或加载）
    public void Initialize()
    {
        // 确保列表长度符合容量，如果是空列表则初始化
        if (slots.Count != MaxCapacity)
        {
            slots.Clear();
            for (int i = 0; i < MaxCapacity; i++)
            {
                slots.Add(new InventorySlot(null, 0));
            }
        }
    }

    /// <summary>
    /// 尝试添加物品到背包
    /// </summary>
    /// <returns>返回是否添加成功</returns>
    public bool AddItem(ItemDataSO item, int amount)
    {
        // 1. 检查是否可堆叠且已存在
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.itemData == item && slot.quantity < item.maxStackSize)
                {
                    int spaceInSlot = item.maxStackSize - slot.quantity;
                    int amountToAdd = Mathf.Min(spaceInSlot, amount);
                    
                    slot.AddQuantity(amountToAdd);
                    amount -= amountToAdd;

                    if (amount <= 0)
                    {
                        OnInventoryUpdated?.Invoke();
                        return true;
                    }
                }
            }
        }

        // 2. 寻找空格子放入剩余物品
        while (amount > 0)
        {
            InventorySlot emptySlot = slots.FirstOrDefault(s => s.IsEmpty);
            
            if (emptySlot != null)
            {
                int amountToAdd = item.isStackable ? Mathf.Min(item.maxStackSize, amount) : 1;
                emptySlot.itemData = item;
                emptySlot.quantity = amountToAdd;
                
                amount -= amountToAdd;
            }
            else
            {
                // 背包满了，部分或全部添加失败
                OnInventoryUpdated?.Invoke();
                return false; // 或者你可以返回剩余未添加的数量
            }
        }

        OnInventoryUpdated?.Invoke();
        return true;
    }
    
    // 增加由僵尸带来的扩容
    public void ExpandCapacity(int amount)
    {
        extraCapacity += amount;
        // 增加新的空槽位
        for(int i = 0; i < amount; i++)
        {
            slots.Add(new InventorySlot(null, 0));
        }
        OnInventoryUpdated?.Invoke();
    }
}