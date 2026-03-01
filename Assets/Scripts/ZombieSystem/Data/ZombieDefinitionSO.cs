using System.Collections.Generic;
using UnityEngine;
using Zompanion.ZombieSystem;

[CreateAssetMenu(fileName = "ZombieDefinition", menuName = "Zombie/Zombie Definition")]
public class ZombieDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string definitionId = "zombie_default";
    [SerializeField] private string displayName = "Zombie";
    [SerializeField] private ZombieType zombieType = ZombieType.Normal;
    [SerializeField] private ZombieCategory category = ZombieCategory.Unknown;

    [Header("Body Part Recipe")]
    [SerializeField] private ZombieBodyParts requiredParts = new ZombieBodyParts();

    [Header("Visual")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite codexIcon;
    [SerializeField] private string codexNumber = "001";

    [Header("Follow")]
    [SerializeField] [Min(0.1f)] private float followMoveSpeed = 2.8f;
    [SerializeField] [Min(0.1f)] private float followDistance = 0.75f;

    [Header("Buff")]
    [SerializeField] private ZombieBuffType buffType = ZombieBuffType.None;
    [SerializeField] private float buffValue = 0f;

    [Header("Codex & Story")]
    [SerializeField] private string storyId = "story_zombie_01";
    [SerializeField] [TextArea] private string shortDescription = "A companion zombie.";

    public string DefinitionId => definitionId;
    public string DisplayName => displayName;
    public ZombieType Type => zombieType;
    public ZombieCategory Category => category;
    public ZombieBodyParts RequiredParts => requiredParts;
    public GameObject Prefab => prefab;
    public Sprite CodexIcon => codexIcon;
    public string CodexNumber => codexNumber;
    public float FollowMoveSpeed => followMoveSpeed;
    public float FollowDistance => followDistance;
    public ZombieBuffType BuffType => buffType;
    public float BuffValue => buffValue;
    public string StoryId => storyId;
    public string ShortDescription => shortDescription;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            definitionId = name.ToLowerInvariant().Replace(" ", "_");
    }
}

[CreateAssetMenu(fileName = "ZombieCatalog", menuName = "Zombie/Zombie Catalog")]
public class ZombieCatalogSO : ScriptableObject
{
    [SerializeField] private List<ZombieDefinitionSO> definitions = new List<ZombieDefinitionSO>();

    public IReadOnlyList<ZombieDefinitionSO> Definitions => definitions;
}

