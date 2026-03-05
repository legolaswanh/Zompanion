using System.Linq;
using Code.Scripts;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using Unity.VisualScripting;

public class DialogueInventoryBridge : MonoBehaviour
{
    [SerializeField] private InventorySO playerInventory;
    [SerializeField] private ItemDataSO[] storyItemsDB;

    private void OnEnable()
    {
        ResolveInventoryReference();

        Lua.RegisterFunction("HasItem", this, SymbolExtensions.GetMethodInfo(() => HasItem(string.Empty, (double)0)));
        Lua.RegisterFunction("GiveItem", this, SymbolExtensions.GetMethodInfo(() => GiveItem(string.Empty, (double)0)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("HasItem");
        Lua.UnregisterFunction("GiveItem");
    }

    /// <summary>
    /// 检查玩家背包中是否拥有指定物品且数量至少为 amount。
    /// itemName 对应 ItemDataSO.itemName；amount 为 1 表示至少 1 个。
    /// </summary>
    public bool HasItem(string itemName, double amount)
    {
        ResolveInventoryReference();
        if (playerInventory == null || string.IsNullOrWhiteSpace(itemName))
            return false;

        ItemDataSO item = ItemLookup.Get(itemName);
        if (item == null)
            return false;

        int count = playerInventory.slots.Count(s => s != null && s.itemData == item);
        return count >= (int)amount;
    }

    /// <summary>
    /// 从玩家背包中移除指定数量的物品。
    /// itemName 对应 ItemDataSO.itemName；amount 为要移除的数量。
    /// </summary>
    public void GiveItem(string itemName, double amount)
    {
        ResolveInventoryReference();
        if (playerInventory == null || string.IsNullOrWhiteSpace(itemName))
            return;

        ItemDataSO item = ItemLookup.Get(itemName);
        if (item == null)
            return;

        int toRemove = Mathf.Max(0, (int)amount);
        for (int i = 0; i < toRemove; i++)
            playerInventory.RemoveItem(item);
    }

    private void ResolveInventoryReference()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInventory == null)
            return;

        playerInventory = GameManager.Instance.PlayerInventory;
    }
}
