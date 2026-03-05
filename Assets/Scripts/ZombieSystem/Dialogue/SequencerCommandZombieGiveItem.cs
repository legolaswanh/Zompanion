using System.Collections;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// <summary>
/// Sequencer 命令：消耗玩家背包中的物品并解锁僵尸剧情。
/// 用法：ZombieGiveItem(itemName)
/// 或：ZombieGiveItem(definitionId, itemName)
/// definitionId 若不传则从 Variable["CurrentZombieDefinitionId"] 读取。
/// 提交成功后自动播放对应的剧情对话。
/// </summary>
public class SequencerCommandZombieGiveItem : SequencerCommand
{
    private static readonly (string StoryId, string ConversationTitle)[] StoryToConversation =
    {
        ("story_zombie_monk_seg1", "ZombieMonk_Segment1"),
        ("story_zombie_monk_seg2", "ZombieMonk_Segment2"),
        ("story_zombie_official_seg1", "ZombieOfficial_Segment1"),
        ("story_zombie_official_seg2", "ZombieOfficial_Segment2"),
    };

    public void Awake()
    {
        string definitionId;
        string itemName;

        if (parameters != null && parameters.Length >= 2)
        {
            definitionId = GetParameter(0);
            itemName = GetParameter(1);
        }
        else if (parameters != null && parameters.Length >= 1)
        {
            definitionId = DialogueLua.GetVariable("CurrentZombieDefinitionId").asString;
            itemName = GetParameter(0);
        }
        else
        {
            Stop();
            return;
        }

        if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(itemName))
        {
            Stop();
            return;
        }

        ItemDataSO item = ItemLookup.Get(itemName);
        if (item == null)
        {
            if (DialogueDebug.logWarnings)
                Debug.LogWarning($"[ZombieGiveItem] Item not found: {itemName}");
            Stop();
            return;
        }

        ZombieStoryService storyService = new ZombieStoryService();
        bool success = storyService.TryGiveItemToZombie(definitionId, item, out string unlockedStoryId);

        if (success && !string.IsNullOrWhiteSpace(unlockedStoryId))
        {
            DialogueLua.SetVariable("story_" + unlockedStoryId, true);

            string conversationTitle = GetConversationForStory(unlockedStoryId);
            if (!string.IsNullOrEmpty(conversationTitle))
            {
                Transform actorTransform = speaker != null ? speaker : DialogueManager.currentActor;
                Transform conversantTransform = listener != null ? listener : DialogueManager.currentConversant;
                if (actorTransform != null && conversantTransform != null)
                {
                    DialogueManager.instance.StartCoroutine(StartSegmentConversationAfterFrame(conversationTitle, actorTransform, conversantTransform));
                }
            }
        }

        Stop();
    }

    private static string GetConversationForStory(string storyId)
    {
        foreach (var pair in StoryToConversation)
        {
            if (pair.StoryId == storyId)
                return pair.ConversationTitle;
        }
        return null;
    }

    private static IEnumerator StartSegmentConversationAfterFrame(string conversationTitle, Transform actor, Transform conversant)
    {
        yield return null;
        if (!DialogueManager.isConversationActive)
        {
            DialogueManager.StartConversation(conversationTitle, actor, conversant);
        }
        else
        {
            DialogueManager.StopConversation();
            yield return null;
            DialogueManager.StartConversation(conversationTitle, actor, conversant);
        }
    }
}
