using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Zompanion.ZombieSystem;

[Serializable]
public class ZombieStorySegmentConfig
{
    [Tooltip("剧情 ID，如 story_zombie_01_seg1")]
    public string storyId;

    [Tooltip("提交此物品解锁该段剧情")]
    public ItemDataSO requiredItem;

    [Tooltip("图鉴中展示的剧情摘要，解锁后显示在头像和名字下方")]
    [TextArea(2, 6)]
    public string storySummary;
}

[CreateAssetMenu(fileName = "ZombieDefinition", menuName = "Zombie/Zombie Definition")]
public class ZombieDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string definitionId = "zombie_default";
    [SerializeField] private string displayName = "Zombie";
    [SerializeField] private ZombieCategory category = ZombieCategory.Unknown;

    [Header("Visual")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite codexIcon;
    [SerializeField] private Sprite codexDetailImage;

    [Header("Follow")]
    [SerializeField] [Min(0.1f)] private float followMoveSpeed = 2.8f;
    [SerializeField] [Min(0.1f)] private float followDistance = 0.75f;

    [Header("Modifier")]
    [SerializeField] private ZombieModifierType modifierType = ZombieModifierType.None;
    [SerializeField] private ZombieModifierApplyMode modifierApplyMode = ZombieModifierApplyMode.OnSpawn;
    [SerializeField] private float modifierValue = 0f;

    // Legacy fields kept only for migration from old assets.
    [FormerlySerializedAs("buffType")] [SerializeField] [HideInInspector] private LegacyZombieBuffType legacyBuffType = LegacyZombieBuffType.None;
    [FormerlySerializedAs("buffValue")] [SerializeField] [HideInInspector] private float legacyBuffValue = 0f;
    [FormerlySerializedAs("abilityType")] [SerializeField] [HideInInspector] private LegacyZombieAbilityType legacyAbilityType = LegacyZombieAbilityType.None;
    [FormerlySerializedAs("abilityValue")] [SerializeField] [HideInInspector] private float legacyAbilityValue = 0f;

    [Header("Codex & Story")]
    [FormerlySerializedAs("storyId")]
    [SerializeField]
    [HideInInspector]
    private string _legacyStoryId = "story_zombie_01";

    [Tooltip("多段剧情配置，每段需提交 requiredItem 解锁")]
    [SerializeField]
    private List<ZombieStorySegmentConfig> storySegments = new List<ZombieStorySegmentConfig>();

    [Tooltip("Dialogue System 中的 Conversation 标题，用于「对话」入口。若为空则使用默认僵尸对话。")]
    [SerializeField] private string dialogueConversationTitle = "ZombieDefault";

    [SerializeField] [TextArea] private string shortDescription;

    public string DefinitionId => definitionId;
    public string DisplayName => displayName;
    public ZombieCategory Category => category;
    public GameObject Prefab => prefab;
    public Sprite CodexIcon => codexIcon;
    public Sprite CodexDetailImage => codexDetailImage;
    public float FollowMoveSpeed => followMoveSpeed;
    public float FollowDistance => followDistance;
    public ZombieModifierType ModifierType => modifierType;
    public ZombieModifierApplyMode ModifierApplyMode => modifierApplyMode;
    public float ModifierValue => modifierValue;

    /// <summary>
    /// 主剧情 ID，用于兼容旧逻辑。若有 storySegments 则返回第一段，否则返回 legacy storyId。
    /// </summary>
    public string StoryId => GetPrimaryStoryId();

    /// <summary>
    /// 多段剧情配置。为空时使用 _legacyStoryId 作为唯一剧情。
    /// </summary>
    public IReadOnlyList<ZombieStorySegmentConfig> StorySegments =>
        storySegments != null && storySegments.Count > 0
            ? storySegments
            : null;

    public string ShortDescription => shortDescription;

    /// <summary>
    /// Dialogue System 中用于与僵尸对话的 Conversation 标题。若为空则使用 "ZombieDefault"。
    /// </summary>
    public string DialogueConversationTitle =>
        string.IsNullOrWhiteSpace(dialogueConversationTitle) ? "ZombieDefault" : dialogueConversationTitle;

    private string GetPrimaryStoryId()
    {
        if (storySegments != null && storySegments.Count > 0)
        {
            var first = storySegments[0];
            if (first != null && !string.IsNullOrWhiteSpace(first.storyId))
                return first.storyId;
        }

        return _legacyStoryId;
    }

    /// <summary>
    /// 根据物品查找对应的剧情 ID，若匹配则返回 storyId，否则返回 null。
    /// </summary>
    public string GetStoryIdForItem(ItemDataSO item)
    {
        if (item == null || storySegments == null)
            return null;

        foreach (var seg in storySegments)
        {
            if (seg == null)
                continue;
            if (seg.requiredItem == item)
                return seg.storyId;
        }

        return null;
    }

    private void OnEnable()
    {
        MigrateLegacyModifierFields();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(definitionId) || definitionId == "zombie_default")
            definitionId = name.ToLowerInvariant().Replace(" ", "_");

        MigrateLegacyModifierFields();
    }

    private void MigrateLegacyModifierFields()
    {
        if (modifierType != ZombieModifierType.None)
            return;

        if (legacyAbilityType != LegacyZombieAbilityType.None)
        {
            modifierType = ZombieModifierType.BackpackCapacity;
            modifierApplyMode = ZombieModifierApplyMode.WhileFollowing;
            modifierValue = legacyAbilityValue;
            return;
        }

        if (legacyBuffType == LegacyZombieBuffType.BackpackCapacity)
        {
            modifierType = ZombieModifierType.BackpackCapacity;
            modifierApplyMode = ZombieModifierApplyMode.OnSpawn;
            modifierValue = legacyBuffValue;
            return;
        }

        if (legacyBuffType == LegacyZombieBuffType.DiggingLootBonus)
        {
            modifierType = ZombieModifierType.DiggingLootBonus;
            modifierApplyMode = ZombieModifierApplyMode.OnSpawn;
            modifierValue = legacyBuffValue;
        }
    }

    private enum LegacyZombieBuffType
    {
        None = 0,
        BackpackCapacity = 1,
        DiggingLootBonus = 2
    }

    private enum LegacyZombieAbilityType
    {
        None = 0,
        FollowBackpackCapacity = 1
    }
}

