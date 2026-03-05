using System.Collections.Generic;
using System.Linq;
using Code.Scripts;
using UnityEngine;

/// <summary>
/// 僵尸剧情服务：处理玩家向僵尸提交物品以解锁剧情，以及检查某僵尸全部剧情是否已解锁。
/// </summary>
public class ZombieStoryService
{
    /// <summary>
    /// 尝试将物品提交给指定僵尸以解锁对应剧情。
    /// </summary>
    /// <param name="zombieDefinitionId">僵尸 definitionId</param>
    /// <param name="item">要提交的物品</param>
    /// <param name="unlockedStoryId">若成功，输出解锁的 storyId；否则为 null</param>
    /// <returns>是否成功消耗物品并解锁剧情</returns>
    public bool TryGiveItemToZombie(string zombieDefinitionId, ItemDataSO item, out string unlockedStoryId)
    {
        unlockedStoryId = null;

        if (string.IsNullOrWhiteSpace(zombieDefinitionId) || item == null)
            return false;

        ZombieManager zombieManager = ZombieManager.Instance;
        if (zombieManager == null)
            return false;

        ZombieDefinitionSO definition = zombieManager.GetDefinition(zombieDefinitionId);
        if (definition == null)
            return false;

        string storyId = definition.GetStoryIdForItem(item);
        if (string.IsNullOrWhiteSpace(storyId))
            return false;

        if (zombieManager.IsStoryUnlocked(storyId))
            return false;

        InventorySO playerInventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        if (playerInventory == null || !HasItem(playerInventory, item))
            return false;

        playerInventory.RemoveItem(item);
        zombieManager.UnlockStory(storyId);
        unlockedStoryId = storyId;
        return true;
    }

    /// <summary>
    /// 检查指定僵尸的全部剧情是否均已解锁。
    /// </summary>
    /// <param name="zombieDefinitionId">僵尸 definitionId</param>
    /// <returns>若该僵尸所有剧情段都已解锁则为 true，否则为 false</returns>
    public bool AreAllStoriesUnlockedForZombie(string zombieDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(zombieDefinitionId))
            return false;

        ZombieManager zombieManager = ZombieManager.Instance;
        if (zombieManager == null)
            return false;

        ZombieDefinitionSO definition = zombieManager.GetDefinition(zombieDefinitionId);
        if (definition == null)
            return false;

        IReadOnlyList<ZombieStorySegmentConfig> segments = definition.StorySegments;
        if (segments != null && segments.Count > 0)
        {
            foreach (ZombieStorySegmentConfig seg in segments)
            {
                if (seg == null || string.IsNullOrWhiteSpace(seg.storyId))
                    continue;
                if (!zombieManager.IsStoryUnlocked(seg.storyId))
                    return false;
            }
            return true;
        }

        string primaryStoryId = definition.StoryId;
        return !string.IsNullOrWhiteSpace(primaryStoryId) && zombieManager.IsStoryUnlocked(primaryStoryId);
    }

    private static bool HasItem(InventorySO inventory, ItemDataSO item)
    {
        if (inventory == null || item == null || inventory.slots == null)
            return false;

        return inventory.slots.Any(s => s != null && s.itemData == item);
    }
}
