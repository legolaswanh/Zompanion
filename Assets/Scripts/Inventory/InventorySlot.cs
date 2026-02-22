using System;

[Serializable]
public class InventorySlot
{
    public ItemDataSO itemData;

    public InventorySlot(ItemDataSO item)
    {
        itemData = item;
    }
    
    // 检查是否为空
    public bool IsEmpty => itemData == null;

    // 清空格子
    public void Clear()
    {
        itemData = null;
    }
}
