using System;
using System.Collections.Generic;
using System.Linq;
using Code.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zompanion.ZombieSystem;

public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private InventorySO playerInventory;
    [SerializeField] private ZombieFollowController followController;
    [SerializeField] private Transform zombieContainer;

    [Header("Config")]
    [SerializeField] [Range(1, 2)] private int maxFollowCount = 2;
    [SerializeField] private bool persistent = true;
    [SerializeField] private bool spawnTestZombiesOnStart;
    [SerializeField] private List<ZombieDefinitionSO> startupDefinitions = new List<ZombieDefinitionSO>();

    private readonly List<ZombieInstanceData> _zombies = new List<ZombieInstanceData>();
    private readonly Dictionary<int, ZombieAgent> _agentByInstanceId = new Dictionary<int, ZombieAgent>();
    private readonly Dictionary<string, ZombieDefinitionSO> _definitionById = new Dictionary<string, ZombieDefinitionSO>();

    private int _nextInstanceId = 1;
    private ZombieCodexService _codexService;

    public event Action OnZombieListChanged;
    public event Action OnCodexChanged;

    public IReadOnlyList<ZombieInstanceData> Zombies => _zombies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistent)
            DontDestroyOnLoad(gameObject);

        _codexService = new ZombieCodexService();
        _codexService.OnCodexUpdated += HandleCodexChanged;

        if (followController == null)
            followController = GetComponentInChildren<ZombieFollowController>(true);
    }

    private void Start()
    {
        RegisterDefinitions(startupDefinitions);
        TryBindPlayerLeader();

        if (!spawnTestZombiesOnStart) return;

        for (int i = 0; i < startupDefinitions.Count; i++)
            SpawnZombie(startupDefinitions[i], i < maxFollowCount);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_codexService != null)
            _codexService.OnCodexUpdated -= HandleCodexChanged;

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindPlayerLeader();
        RebuildFollowQueue();
    }

    public void RegisterDefinitions(IEnumerable<ZombieDefinitionSO> definitions)
    {
        if (definitions == null) return;

        foreach (ZombieDefinitionSO definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId)) continue;
            _definitionById[definition.DefinitionId] = definition;
        }
    }

    public ZombieDefinitionSO GetDefinition(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return null;
        _definitionById.TryGetValue(definitionId, out ZombieDefinitionSO definition);
        return definition;
    }

    public bool TryGetZombie(int instanceId, out ZombieInstanceData zombie)
    {
        zombie = _zombies.FirstOrDefault(z => z.instanceId == instanceId);
        return zombie != null;
    }

    public ZombieInstanceData SpawnZombie(ZombieDefinitionSO definition, bool autoFollow = false)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            return null;

        RegisterDefinitions(new[] { definition });

        ZombieInstanceData data = new ZombieInstanceData(_nextInstanceId++, definition.DefinitionId, definition.DisplayName);
        data.unlockStoryId = definition.StoryId;
        _zombies.Add(data);

        SpawnZombieAgent(data, definition);
        ApplySpawnBuff(data, definition);

        if (autoFollow)
            SetFollowState(data.instanceId, true);

        OnZombieListChanged?.Invoke();
        return data;
    }

    public ZombieInstanceData SpawnZombieByDefinitionId(string definitionId, bool autoFollow = false)
    {
        return SpawnZombie(GetDefinition(definitionId), autoFollow);
    }

    [ContextMenu("Debug Spawn First Startup Zombie")]
    private void DebugSpawnFirstStartupZombie()
    {
        if (startupDefinitions == null || startupDefinitions.Count == 0) return;
        SpawnZombie(startupDefinitions[0], true);
    }

    public bool SetFollowState(int instanceId, bool following)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        if (following)
        {
            if (zombie.state == ZombieState.Following)
                return true;

            if (GetFollowingCount() >= maxFollowCount)
                return false;

            zombie.state = ZombieState.Following;
        }
        else
        {
            if (zombie.state != ZombieState.Following)
                return true;

            zombie.state = ZombieState.Idle;
            zombie.followOrder = -1;
        }

        RebuildFollowQueue();
        OnZombieListChanged?.Invoke();
        return true;
    }

    public bool SetWorkState(int instanceId, bool working)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        zombie.state = working ? ZombieState.Working : ZombieState.Idle;
        zombie.followOrder = -1;

        RebuildFollowQueue();
        OnZombieListChanged?.Invoke();
        return true;
    }

    public bool IsStoryUnlocked(string storyId)
    {
        return _codexService != null && _codexService.IsStoryUnlocked(storyId);
    }

    public bool IsZombieCodexUnlocked(string definitionId)
    {
        return _codexService != null && _codexService.IsZombieUnlocked(definitionId);
    }

    public bool UnlockZombieCodex(string definitionId)
    {
        if (_codexService == null || string.IsNullOrWhiteSpace(definitionId))
            return false;

        bool changed = _codexService.UnlockZombie(definitionId);
        if (changed)
            OnCodexChanged?.Invoke();
        return changed;
    }

    public bool UnlockStory(string storyId)
    {
        if (_codexService == null || string.IsNullOrWhiteSpace(storyId))
            return false;

        bool changed = _codexService.UnlockStory(storyId);
        if (changed)
            OnCodexChanged?.Invoke();
        return changed;
    }

    public bool UnlockZombieAndStory(string definitionId)
    {
        ZombieDefinitionSO definition = GetDefinition(definitionId);
        if (definition == null) return false;

        bool zombieChanged = UnlockZombieCodex(definition.DefinitionId);
        bool storyChanged = UnlockStory(definition.StoryId);
        return zombieChanged || storyChanged;
    }

    private int GetFollowingCount()
    {
        return _zombies.Count(z => z.state == ZombieState.Following);
    }

    private void SpawnZombieAgent(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (definition.Prefab == null) return;

        Transform parent = zombieContainer != null ? zombieContainer : transform;
        GameObject zombieObj = Instantiate(definition.Prefab, parent);
        zombieObj.name = $"Zombie_{data.instanceId}_{definition.DisplayName}";

        ZombieAgent agent = zombieObj.GetComponent<ZombieAgent>();
        if (agent == null)
            agent = zombieObj.AddComponent<ZombieAgent>();

        agent.Initialize(data.instanceId, definition);
        _agentByInstanceId[data.instanceId] = agent;
    }

    private void ApplySpawnBuff(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (data.buffApplied) return;

        data.appliedBuffType = definition.BuffType;
        data.appliedBuffValue = definition.BuffValue;

        switch (definition.BuffType)
        {
            case ZombieBuffType.BackpackCapacity:
            {
                if (playerInventory != null && definition.BuffValue > 0f)
                {
                    int expand = Mathf.RoundToInt(definition.BuffValue);
                    if (expand > 0)
                        playerInventory.ExpandCapacity(expand);
                }
                break;
            }
            case ZombieBuffType.DiggingLootBonus:
                if (definition.BuffValue > 0f)
                    ZombieRuntimeModifiers.AddDiggingLootBonus(definition.BuffValue);
                break;
        }

        data.buffApplied = true;
    }

    private void HandleCodexChanged()
    {
        OnCodexChanged?.Invoke();
    }

    private void RebuildFollowQueue()
    {
        List<ZombieInstanceData> following = _zombies
            .Where(z => z.state == ZombieState.Following)
            .OrderBy(z => z.followOrder < 0 ? int.MaxValue : z.followOrder)
            .ThenBy(z => z.instanceId)
            .Take(maxFollowCount)
            .ToList();

        for (int i = 0; i < following.Count; i++)
            following[i].followOrder = i;

        if (followController == null)
            return;

        var followerAgents = new List<ZombieAgent>(following.Count);
        for (int i = 0; i < following.Count; i++)
        {
            int id = following[i].instanceId;
            if (_agentByInstanceId.TryGetValue(id, out ZombieAgent agent) && agent != null)
                followerAgents.Add(agent);
        }

        followController.SetFollowers(followerAgents);
        TryBindPlayerLeader();
    }

    private void TryBindPlayerLeader()
    {
        if (followController == null) return;

        Transform playerTransform = null;
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            playerTransform = GameManager.Instance.CurrentPlayer.transform;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
            followController.SetLeader(playerTransform);
    }
}
