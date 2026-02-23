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
    [SerializeField] private bool requireCodexUnlockForSpawn = true;
    [SerializeField] private bool spawnTestZombiesOnStart;
    [SerializeField] private List<ZombieDefinitionSO> startupDefinitions = new List<ZombieDefinitionSO>();

    [Header("Spawn Placement")]
    [SerializeField] [Min(0.1f)] private float spawnSideSpacing = 1.15f;
    [SerializeField] [Min(0f)] private float spawnExtraPairSpacing = 0.65f;
    [SerializeField] private float spawnRowYOffset = 0f;
    [SerializeField] [Min(0f)] private float minDistanceToPlayer = 0.8f;
    [SerializeField] [Min(0f)] private float minDistanceToZombie = 0.55f;
    [SerializeField] [Min(0f)] private float spawnCheckRadius = 0.3f;
    [SerializeField] [Min(1)] private int maxSpawnAttempts = 30;
    [SerializeField] [Min(4)] private int fallbackSpiralSteps = 24;
    [SerializeField] [Min(0.01f)] private float spawnSearchStep = 0.22f;
    [SerializeField] private LayerMask spawnBlockedMask;

    private readonly List<ZombieInstanceData> _zombies = new List<ZombieInstanceData>();
    private readonly Dictionary<int, ZombieAgent> _agentByInstanceId = new Dictionary<int, ZombieAgent>();
    private readonly Dictionary<string, ZombieDefinitionSO> _definitionById = new Dictionary<string, ZombieDefinitionSO>();
    private readonly Collider2D[] _spawnOverlapBuffer = new Collider2D[32];

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

        int startupSpawnCount = Mathf.Min(maxFollowCount, startupDefinitions.Count);
        for (int i = 0; i < startupSpawnCount; i++)
            SpawnZombie(startupDefinitions[i], true, ignoreCodexUnlock: true);
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
        SyncPlayerZombieCollisionIgnores();
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

    public bool CanSpawnByDefinitionId(string definitionId)
    {
        ZombieDefinitionSO definition = GetDefinition(definitionId);
        if (definition == null) return false;
        return !requireCodexUnlockForSpawn || IsZombieCodexUnlocked(definition.DefinitionId);
    }

    public bool TryGetZombie(int instanceId, out ZombieInstanceData zombie)
    {
        zombie = _zombies.FirstOrDefault(z => z.instanceId == instanceId);
        return zombie != null;
    }

    public ZombieInstanceData SpawnZombie(ZombieDefinitionSO definition, bool autoFollow = false, bool ignoreCodexUnlock = false)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            return null;

        if (requireCodexUnlockForSpawn && !ignoreCodexUnlock && !IsZombieCodexUnlocked(definition.DefinitionId))
        {
            Debug.LogWarning($"[ZombieManager] Spawn blocked: codex not unlocked for '{definition.DefinitionId}'.");
            return null;
        }

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

    public ZombieInstanceData SpawnZombieByDefinitionId(string definitionId, bool autoFollow = false, bool ignoreCodexUnlock = false)
    {
        return SpawnZombie(GetDefinition(definitionId), autoFollow, ignoreCodexUnlock);
    }

    [ContextMenu("Debug Spawn First Startup Zombie")]
    private void DebugSpawnFirstStartupZombie()
    {
        if (startupDefinitions == null || startupDefinitions.Count == 0) return;
        SpawnZombie(startupDefinitions[0], true, ignoreCodexUnlock: true);
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

    private int GetFollowingCount()
    {
        return _zombies.Count(z => z.state == ZombieState.Following);
    }

    private void SpawnZombieAgent(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (definition.Prefab == null) return;

        Transform parent = zombieContainer != null ? zombieContainer : transform;
        Vector3 spawnPosition = ResolveSpawnPosition(parent);
        GameObject zombieObj = Instantiate(definition.Prefab, spawnPosition, Quaternion.identity, parent);
        zombieObj.name = $"Zombie_{data.instanceId}_{definition.DisplayName}";

        ZombieAgent agent = zombieObj.GetComponent<ZombieAgent>();
        if (agent == null)
            agent = zombieObj.AddComponent<ZombieAgent>();

        agent.Initialize(data.instanceId, definition);
        _agentByInstanceId[data.instanceId] = agent;
        SyncPlayerZombieCollisionIgnores();
    }

    private Vector3 ResolveSpawnPosition(Transform parent)
    {
        Vector3 center = GetSpawnCenter(parent);
        center.z = parent != null ? parent.position.z : center.z;

        int spawnIndex = Mathf.Max(0, _agentByInstanceId.Count);
        Vector3 preferred = GetAlternatingSpawnPoint(center, spawnIndex);
        Transform player = GetPlayerTransform();
        if (IsSpawnPositionValid(preferred, player))
            return preferred;

        const float goldenAngle = 2.39996323f;
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float angle = i * goldenAngle;
            float radius = spawnSearchStep * (i + 1);
            Vector3 candidate = preferred + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            if (IsSpawnPositionValid(candidate, player))
                return candidate;
        }

        float spiralStep = Mathf.Max(spawnSearchStep, spawnCheckRadius * 1.5f);
        for (int i = 0; i < fallbackSpiralSteps; i++)
        {
            float angle = i * goldenAngle;
            float radius = spawnSideSpacing + i * spiralStep;
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            if (IsSpawnPositionValid(candidate, player))
                return candidate;
        }

        return preferred;
    }

    private Vector3 GetSpawnCenter(Transform parent)
    {
        Transform player = GetPlayerTransform();
        if (player != null)
            return player.position;

        if (parent != null)
            return parent.position;

        return transform.position;
    }

    private Vector3 GetAlternatingSpawnPoint(Vector3 center, int spawnIndex)
    {
        bool spawnLeft = (spawnIndex % 2) == 0;
        int row = spawnIndex / 2;
        float x = (spawnLeft ? -1f : 1f) * (spawnSideSpacing + row * spawnExtraPairSpacing);
        float y = spawnRowYOffset * row;
        return center + new Vector3(x, y, 0f);
    }

    private bool IsSpawnPositionValid(Vector3 position, Transform player)
    {
        if (player != null && minDistanceToPlayer > 0f)
        {
            float distanceToPlayer = Vector2.Distance(position, player.position);
            if (distanceToPlayer < minDistanceToPlayer)
                return false;
        }

        if (minDistanceToZombie > 0f)
        {
            foreach (var pair in _agentByInstanceId)
            {
                ZombieAgent agent = pair.Value;
                if (agent == null) continue;

                float distanceToZombie = Vector2.Distance(position, agent.transform.position);
                if (distanceToZombie < minDistanceToZombie)
                    return false;
            }
        }

        if (spawnCheckRadius <= 0f || spawnBlockedMask.value == 0)
            return true;

        var filter = new ContactFilter2D
        {
            useLayerMask = spawnBlockedMask.value != 0,
            layerMask = spawnBlockedMask,
            useTriggers = false
        };
        int hitCount = Physics2D.OverlapCircle(position, spawnCheckRadius, filter, _spawnOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _spawnOverlapBuffer[i];
            if (hit == null || hit.isTrigger) continue;
            return false;
        }

        return true;
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

        Transform playerTransform = GetPlayerTransform();

        if (playerTransform != null)
            followController.SetLeader(playerTransform);
    }

    private void SyncPlayerZombieCollisionIgnores()
    {
        Collider2D[] playerColliders = GetPlayerColliders();
        if (playerColliders == null || playerColliders.Length == 0) return;

        foreach (var pair in _agentByInstanceId)
        {
            ZombieAgent agent = pair.Value;
            if (agent == null) continue;

            Collider2D[] zombieColliders = agent.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider2D playerCol = playerColliders[i];
                if (playerCol == null) continue;

                for (int j = 0; j < zombieColliders.Length; j++)
                {
                    Collider2D zombieCol = zombieColliders[j];
                    if (zombieCol == null) continue;
                    Physics2D.IgnoreCollision(playerCol, zombieCol, true);
                }
            }
        }
    }

    private Collider2D[] GetPlayerColliders()
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null) return null;
        return playerTransform.GetComponentsInChildren<Collider2D>(true);
    }

    private Transform GetPlayerTransform()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            return GameManager.Instance.CurrentPlayer.transform;

        GameObject playerObj = GameObject.FindWithTag("Player");
        return playerObj != null ? playerObj.transform : null;
    }
}
