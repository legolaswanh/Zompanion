using System.Linq;
using Code.Scripts;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// 为 Dialogue System 的 Lua 提供僵尸相关函数。
/// HasItemForZombie(definitionId, itemName)：玩家有该物品 且 该僵尸需要它解锁剧情 且 剧情未解锁 时返回 true。
/// 用于对话选项的 Condition，以显示「提交 XXX」选项。
/// </summary>
public class ZombieDialogueLuaBridge : MonoBehaviour
{
    private static ZombieStoryService _storyService;
    private static ZombieStoryService StoryService => _storyService ??= new ZombieStoryService();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Lua.RegisterFunction("HasItemForZombie", this, SymbolExtensions.GetMethodInfo(() => HasItemForZombie(string.Empty, string.Empty)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("HasItemForZombie");
    }

    /// <summary>
    /// Lua 可调用：HasItemForZombie(definitionId, itemName)
    /// 玩家有物品 且 僵尸需要该物品解锁剧情 且 该剧情未解锁 时返回 true。
    /// </summary>
    public bool HasItemForZombie(string definitionId, string itemName)
    {
        if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(itemName))
            return false;
        definitionId = definitionId.Trim();
        itemName = itemName.Trim();

        ZombieManager zm = ZombieManager.Instance;
        if (zm == null)
            return false;

        ZombieDefinitionSO definition = zm.GetDefinition(definitionId);
        if (definition == null)
            return false;

        ItemDataSO item = ItemLookup.Get(itemName);
        if (item == null)
            return false;

        string storyId = definition.GetStoryIdForItem(item);
        if (string.IsNullOrWhiteSpace(storyId))
            return false;

        if (zm.IsStoryUnlocked(storyId))
            return false;

        InventorySO inv = GameManager.Instance?.PlayerInventory;
        if (inv == null || inv.slots == null)
            return false;

        return inv.slots.Any(s => s != null && !s.IsEmpty && s.itemData == item);
    }
}
