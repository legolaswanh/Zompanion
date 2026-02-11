using System;

[Serializable]
public class InventorySlot
{
    public ItemDataSO itemData;
    public int quantity;

    public InventorySlot(ItemDataSO item, int qty)
    {
        itemData = item;
        quantity = qty;
    }

    public void AddQuantity(int amount) => quantity += amount;
    
    // 检查是否为空
    public bool IsEmpty => itemData == null || quantity <= 0;

    // 清空格子
    public void Clear()
    {
        itemData = null;
        quantity = 0;
    }
}
