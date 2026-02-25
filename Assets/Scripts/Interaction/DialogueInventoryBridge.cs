using UnityEngine;
using PixelCrushers.DialogueSystem;

public class DialogueInventoryBridge : MonoBehaviour
{
    [SerializeField] private InventorySO playerInventory;
    [SerializeField] private ItemDataSO[] storyItemsDB; // 粗暴点，把所有物品SO拖进来以便名字查找

    private void OnEnable()
    {
        // 将 C# 方法注册到 Lua 中
        Lua.RegisterFunction("HasItem", this, SymbolExtensions.GetMethodInfo(() => HasItem(string.Empty, (double)0)));
        Lua.RegisterFunction("GiveItem", this, SymbolExtensions.GetMethodInfo(() => GiveItem(string.Empty, (double)0)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("HasItem");
        Lua.UnregisterFunction("GiveItem");
    }

    // 供 Lua 调用的方法：检查是否有足够的物品
    public bool HasItem(string itemName, double amount)
    {
        // 这里你需要遍历 playerInventory.slots，检查 itemName 的数量是否 >= amount
        // 返回 true 或 false
        return false; 
    }

    // 供 Lua 调用的方法：给玩家物品
    public void GiveItem(string itemName, double amount)
    {
        // 根据 itemName 从 allItemsDB 找到对应的 ItemDataSO
        // 然后调用 playerInventory.AddItem(item, (int)amount);
    }
}