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
    [SerializeField] private ZombieFollowController followController;
    [SerializeField] private Transform zombieContainer;

    [Header("Config")]
    [SerializeField] [Range(1, 2)] private int maxFollowCount = 2;
    [SerializeField] private bool persistent = true;
    [SerializeField] private bool requireCodexUnlockForSpawn = true;

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
    private readonly List<ZombieDefinitionSO> _catalogDefinitions = new List<ZombieDefinitionSO>();
    private readonly Collider2D[] _spawnOverlapBuffer = new Collider2D[32];
    private ZombieCatalogSO zombieCatalog;
    private InventorySO playerInventory;

    private int _nextInstanceId = 1;
    private ZombieCodexService _codexService;

    public event Action OnZombieListChanged;
    public event Action OnCodexChanged;

    public IReadOnlyList<ZombieInstanceData> Zombies => _zombies;
    public IReadOnlyList<ZombieDefinitionSO> CatalogDefinitions => _catalogDefinitions;
    public int MaxFollowCount => maxFollowCount;

    public void SetPlayerInventory(InventorySO inventory)
    {
        playerInventory = inventory;
    }

    public void SetZombieCatalog(ZombieCatalogSO catalog)
    {
        zombieCatalog = catalog;
        RebuildCatalogDefinitions();
    }

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

        ResolveRuntimeReferences();

        if (zombieCatalog != null)
            RebuildCatalogDefinitions();
    }

    private void Start()
    {
        ResolveRuntimeReferences();
        TryBindPlayerLeader();
        UpdateAgentScenePresence(applyHomeSnap: true);
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
        ResolveRuntimeReferences();
        TryBindPlayerLeader();
        ReparentAllAgentsToContainer();
        UpdateAgentScenePresence(applyHomeSnap: true);
        SyncPlayerZombieCollisionIgnores();
        RebuildFollowQueue();
    }

    private void ResolveRuntimeReferences()
    {
        if (GameManager.Instance != null)
        {
            if (playerInventory == null)
                playerInventory = GameManager.Instance.PlayerInventory;

            if (zombieCatalog == null)
            {
                ZombieCatalogSO configCatalog = GameManager.Instance.ZombieCatalog;
                if (configCatalog != null)
                {
                    zombieCatalog = configCatalog;
                    RebuildCatalogDefinitions();
                }
            }
        }

        if (followController == null)
            followController = GetComponentInChildren<ZombieFollowController>(true);
    }

    public void RegisterDefinitions(IEnumerable<ZombieDefinitionSO> definitions)
    {
        if (definitions == null) return;

        foreach (ZombieDefinitionSO definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId)) continue;

            if (_definitionById.TryGetValue(definition.DefinitionId, out ZombieDefinitionSO existingById) &&
                existingById != null &&
                existingById != definition)
            {
                Debug.LogWarning($"[ZombieManager] Duplicate zombie definitionId '{definition.DefinitionId}'. Using latest definition asset '{definition.name}'.");
            }

            _definitionById[definition.DefinitionId] = definition;

            bool exists = false;
            for (int i = 0; i < _catalogDefinitions.Count; i++)
            {
                ZombieDefinitionSO existing = _catalogDefinitions[i];
                if (existing == null) continue;
                if (existing.DefinitionId != definition.DefinitionId) continue;
                _catalogDefinitions[i] = definition;
                exists = true;
                break;
            }

            if (!exists)
                _catalogDefinitions.Add(definition);
        }
    }

    private void RebuildCatalogDefinitions()
    {
        _definitionById.Clear();
        _catalogDefinitions.Clear();

        if (zombieCatalog == null || zombieCatalog.Definitions == null)
            return;

        RegisterDefinitions(zombieCatalog.Definitions);
        Debug.Log($"[ZombieManager] Catalog initialized. Source count: {zombieCatalog.Definitions.Count}, unique definitionId count: {_catalogDefinitions.Count}.");
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

    public bool HasZombieByDefinitionId(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        return _zombies.Any(z => z.definitionId == definitionId);
    }

    public bool IsDefinitionFollowing(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return false;
        return _zombies.Any(z => z.definitionId == definitionId && z.state == ZombieState.Following);
    }

    public bool IsDefinitionWorking(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return false;
        return _zombies.Any(z => z.definitionId == definitionId && z.state == ZombieState.Working);
    }

    public List<string> GetFollowingDefinitionIdsOrdered()
    {
        return _zombies
            .Where(z => z.state == ZombieState.Following)
            .OrderBy(z => z.followOrder < 0 ? int.MaxValue : z.followOrder)
            .ThenBy(z => z.instanceId)
            .Take(maxFollowCount)
            .Select(z => z.definitionId)
            .ToList();
    }

    public bool TrySetFollowByDefinitionId(string definitionId, bool follow)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        if (!follow)
        {
            ZombieInstanceData following = _zombies.FirstOrDefault(
                z => z.definitionId == definitionId && z.state == ZombieState.Following);
            if (following == null)
                return true;

            return SetFollowState(following.instanceId, false);
        }

        if (!IsZombieCodexUnlocked(definitionId))
            return false;

        if (IsDefinitionFollowing(definitionId))
            return true;

        ZombieInstanceData existing = _zombies.FirstOrDefault(z => z.definitionId == definitionId);
        if (existing == null)
            existing = SpawnZombieByDefinitionId(definitionId, autoFollow: false, ignoreCodexUnlock: false);

        if (existing == null)
            return false;

        return SetFollowState(existing.instanceId, true);
    }

    public bool TryToggleFollowByDefinitionId(string definitionId)
    {
        return TrySetFollowByDefinitionId(definitionId, !IsDefinitionFollowing(definitionId));
    }

    public bool TryGetZombie(int instanceId, out ZombieInstanceData zombie)
    {
        zombie = _zombies.FirstOrDefault(z => z.instanceId == instanceId);
        return zombie != null;
    }

    public bool SetZombieHomeAnchor(int instanceId, string sceneName, Vector3 position, Vector3 eulerAngles)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = GetActiveSceneName();

        zombie.hasHomeAnchor = true;
        zombie.homeSceneName = sceneName;
        zombie.homePosition = position;
        zombie.homeEulerAngles = eulerAngles;
        zombie.pendingReturnToHome = false;

        UpdateAgentScenePresence(applyHomeSnap: false);
        OnZombieListChanged?.Invoke();
        return true;
    }

    public bool CaptureZombieCurrentTransformAsHomeAnchor(int instanceId)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        if (!_agentByInstanceId.TryGetValue(instanceId, out ZombieAgent agent) || agent == null)
            return false;

        return SetZombieHomeAnchor(
            instanceId,
            GetActiveSceneName(),
            agent.transform.position,
            agent.transform.eulerAngles);
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

        ZombieInstanceData existing = _zombies.FirstOrDefault(z => z.definitionId == definition.DefinitionId);
        if (existing != null)
        {
            existing.displayName = definition.DisplayName;
            existing.unlockStoryId = definition.StoryId;

            if (autoFollow || ShouldZombieBeVisibleInScene(existing, GetActiveSceneName()))
                EnsureZombieAgent(existing, definition);

            EnsureHomeAnchorIfMissing(existing);

            bool handledByFollowState = false;
            if (autoFollow)
                handledByFollowState = SetFollowState(existing.instanceId, true);
            else if (existing.state == ZombieState.Following)
                RebuildFollowQueue();

            UpdateAgentScenePresence(applyHomeSnap: false);

            if (!handledByFollowState)
                OnZombieListChanged?.Invoke();
            return existing;
        }

        ZombieInstanceData data = new ZombieInstanceData(_nextInstanceId++, definition.DefinitionId, definition.DisplayName);
        data.unlockStoryId = definition.StoryId;
        _zombies.Add(data);

        EnsureZombieAgent(data, definition);
        EnsureHomeAnchorIfMissing(data);
        ApplySpawnModifier(data, definition);

        if (autoFollow)
            SetFollowState(data.instanceId, true);

        UpdateAgentScenePresence(applyHomeSnap: false);
        OnZombieListChanged?.Invoke();
        return data;
    }

    public ZombieInstanceData SpawnZombieByDefinitionId(string definitionId, bool autoFollow = false, bool ignoreCodexUnlock = false)
    {
        return SpawnZombie(GetDefinition(definitionId), autoFollow, ignoreCodexUnlock);
    }

    [ContextMenu("Debug Spawn First Catalog Zombie")]
    private void DebugSpawnFirstCatalogZombie()
    {
        if (_catalogDefinitions.Count == 0)
            RebuildCatalogDefinitions();

        if (_catalogDefinitions.Count == 0)
            return;

        ZombieDefinitionSO definition = _catalogDefinitions[0];
        if (definition == null)
            return;

        SpawnZombie(definition, true, ignoreCodexUnlock: true);
    }

    [ContextMenu("Debug Unlock All Zombie Codex")]
    private void DebugUnlockAllZombieCodex()
    {
        if (_catalogDefinitions.Count == 0)
            RebuildCatalogDefinitions();

        int unlockedCount = 0;
        for (int i = 0; i < _catalogDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _catalogDefinitions[i];
            if (definition == null) continue;
            if (UnlockZombieCodex(definition.DefinitionId))
                unlockedCount++;
        }

        Debug.Log($"[ZombieManager] Debug unlocked zombie codex entries: {unlockedCount}/{_catalogDefinitions.Count}.");
    }

    [ContextMenu("Debug Unlock All Zombie Codex + Story")]
    private void DebugUnlockAllZombieCodexAndStory()
    {
        if (_catalogDefinitions.Count == 0)
            RebuildCatalogDefinitions();

        int unlockedZombieCount = 0;
        int unlockedStoryCount = 0;
        for (int i = 0; i < _catalogDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _catalogDefinitions[i];
            if (definition == null) continue;

            if (UnlockZombieCodex(definition.DefinitionId))
                unlockedZombieCount++;

            if (UnlockStory(definition.StoryId))
                unlockedStoryCount++;
        }

        Debug.Log($"[ZombieManager] Debug unlocked zombie codex entries: {unlockedZombieCount}/{_catalogDefinitions.Count}, stories: {unlockedStoryCount}");
    }

    public bool SetFollowState(int instanceId, bool following)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        ZombieDefinitionSO definition = GetDefinition(zombie.definitionId);

        if (following)
        {
            if (zombie.state == ZombieState.Following)
            {
                EnsureZombieAgent(zombie, definition);
                if (_agentByInstanceId.TryGetValue(zombie.instanceId, out ZombieAgent existingFollowAgent) &&
                    existingFollowAgent != null &&
                    !existingFollowAgent.gameObject.activeSelf)
                {
                    existingFollowAgent.gameObject.SetActive(true);
                }

                UpdateAgentScenePresence(applyHomeSnap: false);
                return true;
            }

            if (GetFollowingCount() >= maxFollowCount)
                return false;

            EnsureZombieAgent(zombie, definition);
            EnsureHomeAnchorIfMissing(zombie);
            zombie.pendingReturnToHome = false;

            if (_agentByInstanceId.TryGetValue(zombie.instanceId, out ZombieAgent followAgent) &&
                followAgent != null &&
                !followAgent.gameObject.activeSelf)
            {
                followAgent.gameObject.SetActive(true);
            }

            zombie.state = ZombieState.Following;
            ApplyFollowModifier(zombie, definition);
        }
        else
        {
            if (zombie.state != ZombieState.Following)
                return true;

            RemoveFollowModifier(zombie);
            zombie.state = ZombieState.Idle;
            zombie.followOrder = -1;
            HandleTransitionToNonFollowingState(zombie);
        }

        RebuildFollowQueue();
        UpdateAgentScenePresence(applyHomeSnap: false);
        OnZombieListChanged?.Invoke();
        return true;
    }

    public bool SetWorkState(int instanceId, bool working)
    {
        if (!TryGetZombie(instanceId, out ZombieInstanceData zombie))
            return false;

        if (zombie.state == ZombieState.Following)
            RemoveFollowModifier(zombie);

        zombie.state = working ? ZombieState.Working : ZombieState.Idle;
        zombie.followOrder = -1;
        HandleTransitionToNonFollowingState(zombie);

        RebuildFollowQueue();
        UpdateAgentScenePresence(applyHomeSnap: false);
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

        return _codexService.UnlockZombie(definitionId);
    }

    public bool UnlockStory(string storyId)
    {
        if (_codexService == null || string.IsNullOrWhiteSpace(storyId))
            return false;

        return _codexService.UnlockStory(storyId);
    }

    private int GetFollowingCount()
    {
        return _zombies.Count(z => z.state == ZombieState.Following);
    }

    private void SpawnZombieAgent(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (definition.Prefab == null) return;

        Transform parent = GetZombieContainer();
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

    private void EnsureZombieAgent(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (data == null || definition == null)
            return;

        if (!_agentByInstanceId.TryGetValue(data.instanceId, out ZombieAgent agent) || agent == null)
        {
            SpawnZombieAgent(data, definition);
            return;
        }

        Transform parent = GetZombieContainer();
        if (agent.transform.parent != parent)
            agent.transform.SetParent(parent, true);

        agent.name = $"Zombie_{data.instanceId}_{definition.DisplayName}";
        agent.Initialize(data.instanceId, definition);
    }

    private Transform GetZombieContainer()
    {
        if (zombieContainer != null)
            return zombieContainer;

        GameObject container = new GameObject("ZombieContainer");
        zombieContainer = container.transform;
        zombieContainer.SetParent(transform, false);
        return zombieContainer;
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

    private void ApplySpawnModifier(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (data == null || definition == null)
            return;

        if (definition.ModifierApplyMode != ZombieModifierApplyMode.OnSpawn)
            return;

        if (data.modifierApplied && data.appliedModifierApplyMode == ZombieModifierApplyMode.OnSpawn)
            return;

        ApplyModifierInternal(data, definition.ModifierType, definition.ModifierApplyMode, definition.ModifierValue);
    }

    private void ApplyFollowModifier(ZombieInstanceData data, ZombieDefinitionSO definition)
    {
        if (data == null || definition == null)
            return;

        if (definition.ModifierApplyMode != ZombieModifierApplyMode.WhileFollowing)
            return;

        if (data.modifierApplied && data.appliedModifierApplyMode == ZombieModifierApplyMode.WhileFollowing)
            return;

        ApplyModifierInternal(data, definition.ModifierType, definition.ModifierApplyMode, definition.ModifierValue);
    }

    private void RemoveFollowModifier(ZombieInstanceData data)
    {
        if (data == null || !data.modifierApplied)
            return;

        if (data.appliedModifierApplyMode != ZombieModifierApplyMode.WhileFollowing)
            return;

        RemoveModifierEffect(data.appliedModifierType, data.appliedModifierValue);
        ClearAppliedModifier(data);
    }

    private void ApplyModifierInternal(
        ZombieInstanceData data,
        ZombieModifierType modifierType,
        ZombieModifierApplyMode applyMode,
        float modifierValue)
    {
        if (modifierType == ZombieModifierType.None || modifierValue <= 0f)
            return;

        ApplyModifierEffect(modifierType, modifierValue);
        data.modifierApplied = true;
        data.appliedModifierType = modifierType;
        data.appliedModifierApplyMode = applyMode;
        data.appliedModifierValue = modifierValue;
    }

    private void ApplyModifierEffect(ZombieModifierType modifierType, float modifierValue)
    {
        switch (modifierType)
        {
            case ZombieModifierType.BackpackCapacity:
            {
                if (playerInventory == null)
                    return;

                int expand = Mathf.RoundToInt(modifierValue);
                if (expand > 0)
                    playerInventory.ExpandCapacity(expand);
                break;
            }
            case ZombieModifierType.DiggingLootBonus:
                ZombieRuntimeModifiers.AddDiggingLootBonus(modifierValue);
                break;
        }
    }

    private void RemoveModifierEffect(ZombieModifierType modifierType, float modifierValue)
    {
        switch (modifierType)
        {
            case ZombieModifierType.BackpackCapacity:
            {
                if (playerInventory == null)
                    return;

                int shrink = Mathf.RoundToInt(modifierValue);
                if (shrink > 0)
                    playerInventory.ExpandCapacity(-shrink);
                break;
            }
            case ZombieModifierType.DiggingLootBonus:
                ZombieRuntimeModifiers.AddDiggingLootBonus(-modifierValue);
                break;
        }
    }

    private static void ClearAppliedModifier(ZombieInstanceData data)
    {
        data.modifierApplied = false;
        data.appliedModifierType = ZombieModifierType.None;
        data.appliedModifierApplyMode = ZombieModifierApplyMode.OnSpawn;
        data.appliedModifierValue = 0f;
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

    private void EnsureHomeAnchorIfMissing(ZombieInstanceData zombie)
    {
        if (zombie == null || zombie.hasHomeAnchor)
            return;

        Vector3 anchorPosition;
        Vector3 anchorEuler;
        if (_agentByInstanceId.TryGetValue(zombie.instanceId, out ZombieAgent agent) && agent != null)
        {
            anchorPosition = agent.transform.position;
            anchorEuler = agent.transform.eulerAngles;
        }
        else
        {
            anchorPosition = GetSpawnCenter(GetZombieContainer());
            anchorEuler = Vector3.zero;
        }

        zombie.homeSceneName = GetActiveSceneName();
        zombie.homePosition = anchorPosition;
        zombie.homeEulerAngles = anchorEuler;
        zombie.hasHomeAnchor = true;
        zombie.pendingReturnToHome = false;
    }

    private void HandleTransitionToNonFollowingState(ZombieInstanceData zombie)
    {
        if (zombie == null)
            return;

        EnsureHomeAnchorIfMissing(zombie);
        if (!zombie.hasHomeAnchor || string.IsNullOrWhiteSpace(zombie.homeSceneName))
            return;

        string activeSceneName = GetActiveSceneName();
        bool isAwayFromHome = !string.Equals(activeSceneName, zombie.homeSceneName, StringComparison.Ordinal);
        if (!isAwayFromHome)
        {
            zombie.pendingReturnToHome = false;
            return;
        }

        zombie.pendingReturnToHome = true;
        if (!_agentByInstanceId.TryGetValue(zombie.instanceId, out ZombieAgent agent) || agent == null)
            return;

        agent.transform.SetPositionAndRotation(zombie.homePosition, Quaternion.Euler(zombie.homeEulerAngles));
        if (agent.gameObject.activeSelf)
            agent.gameObject.SetActive(false);
    }

    private bool ShouldZombieBeVisibleInScene(ZombieInstanceData zombie, string sceneName)
    {
        if (zombie == null)
            return false;

        if (zombie.state == ZombieState.Following)
            return true;

        if (!zombie.hasHomeAnchor || string.IsNullOrWhiteSpace(zombie.homeSceneName))
            return true;

        return string.Equals(zombie.homeSceneName, sceneName, StringComparison.Ordinal);
    }

    private void UpdateAgentScenePresence(bool applyHomeSnap)
    {
        CleanupMissingAgents();
        string activeSceneName = GetActiveSceneName();

        for (int i = 0; i < _zombies.Count; i++)
        {
            ZombieInstanceData zombie = _zombies[i];
            if (zombie == null)
                continue;

            bool shouldBeVisible = ShouldZombieBeVisibleInScene(zombie, activeSceneName);
            if (!_agentByInstanceId.TryGetValue(zombie.instanceId, out ZombieAgent agent) || agent == null)
            {
                if (!shouldBeVisible)
                    continue;

                ZombieDefinitionSO definition = GetDefinition(zombie.definitionId);
                if (definition == null || definition.Prefab == null)
                    continue;

                EnsureZombieAgent(zombie, definition);
                _agentByInstanceId.TryGetValue(zombie.instanceId, out agent);
            }

            if (agent == null)
                continue;

            if (!shouldBeVisible)
            {
                if (agent.gameObject.activeSelf)
                    agent.gameObject.SetActive(false);
                continue;
            }

            if (!agent.gameObject.activeSelf)
                agent.gameObject.SetActive(true);

            bool inHomeScene = zombie.hasHomeAnchor &&
                               !string.IsNullOrWhiteSpace(zombie.homeSceneName) &&
                               string.Equals(zombie.homeSceneName, activeSceneName, StringComparison.Ordinal);

            if (zombie.state != ZombieState.Following && inHomeScene && (applyHomeSnap || zombie.pendingReturnToHome))
            {
                agent.transform.SetPositionAndRotation(zombie.homePosition, Quaternion.Euler(zombie.homeEulerAngles));
                zombie.pendingReturnToHome = false;
            }
        }
    }

    private static string GetActiveSceneName()
    {
        Scene active = SceneManager.GetActiveScene();
        return active.IsValid() ? active.name : string.Empty;
    }

    private void ReparentAllAgentsToContainer()
    {
        CleanupMissingAgents();

        Transform parent = GetZombieContainer();
        foreach (var pair in _agentByInstanceId)
        {
            ZombieAgent agent = pair.Value;
            if (agent == null)
                continue;

            if (agent.transform.parent != parent)
                agent.transform.SetParent(parent, true);
        }
    }

    private void CleanupMissingAgents()
    {
        List<int> deadIds = null;
        foreach (var pair in _agentByInstanceId)
        {
            if (pair.Value != null)
                continue;

            if (deadIds == null)
                deadIds = new List<int>();
            deadIds.Add(pair.Key);
        }

        if (deadIds == null)
            return;

        for (int i = 0; i < deadIds.Count; i++)
            _agentByInstanceId.Remove(deadIds[i]);
    }
}
