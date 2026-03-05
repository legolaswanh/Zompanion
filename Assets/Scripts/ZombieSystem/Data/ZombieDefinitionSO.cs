using UnityEngine;
using UnityEngine.Serialization;
using Zompanion.ZombieSystem;

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
    [SerializeField] private string storyId = "story_zombie_01";
    [SerializeField] [TextArea] private string shortDescription = "A companion zombie.";

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
    public string StoryId => storyId;
    public string ShortDescription => shortDescription;

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

