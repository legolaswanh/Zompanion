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

    public bool HasItem(string itemName, double amount)
    {
        return false;
    }

    public void GiveItem(string itemName, double amount)
    {
    }

    private void ResolveInventoryReference()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInventory == null)
            return;

        playerInventory = GameManager.Instance.PlayerInventory;
    }
}
