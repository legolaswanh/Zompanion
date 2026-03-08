using System;
using System.Collections.Generic;

/// <summary>
/// 储物数据接口，供背包、宝箱等共用。
/// 使 InventorySlotUI 能同时绑定玩家背包和宝箱储物。
/// </summary>
public interface IStorageData
{
    List<InventorySlot> Slots { get; }

    void Initialize();

    void SetItemAt(int index, ItemDataSO item);

    void RemoveItem(ItemDataSO item);

    event Action OnInventoryUpdated;
}
