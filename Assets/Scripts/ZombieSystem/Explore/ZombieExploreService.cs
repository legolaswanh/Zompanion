using System;
using System.Collections.Generic;
using UnityEngine;
using Zompanion.ZombieSystem;

/// <summary>
/// 管理 Official 僵尸外出探索逻辑：倒计时、寄回物品、待领取状态。
/// 挂载在 ZombieSystem 上，与 ZombieManager 配合使用。
/// </summary>
public class ZombieExploreService : MonoBehaviour
{
    public static ZombieExploreService Instance { get; private set; }

    /// <summary>礼物到达时触发，参数为物品和发送该物品的僵尸 definitionId。</summary>
    public event Action<ItemDataSO, string> OnGiftArrived;

    /// <summary>玩家领取礼物后触发。</summary>
    public event Action OnGiftClaimed;

    private string _exploringDefinitionId;
    private float _nextGiftTime = -1f;
    private ItemDataSO _pendingItem;
    private string _pendingDefinitionId;
    private ZombieManager _zombieManager;

    /// <summary>当前是否有待领取的礼物。</summary>
    public bool HasPendingGift => _pendingItem != null;

    /// <summary>当前待领取的物品。</summary>
    public ItemDataSO PendingItem => _pendingItem;

    /// <summary>发送待领取物品的僵尸 definitionId。</summary>
    public string PendingDefinitionId => _pendingDefinitionId;

    /// <summary>指定僵尸是否正在探索。</summary>
    public bool IsExploring(string definitionId) =>
        !string.IsNullOrWhiteSpace(definitionId) && _exploringDefinitionId == definitionId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _zombieManager = ZombieManager.Instance;
    }

    private void Update()
    {
        if (_zombieManager == null)
            _zombieManager = ZombieManager.Instance;

        if (string.IsNullOrWhiteSpace(_exploringDefinitionId) || _pendingItem != null)
            return;

        if (_nextGiftTime < 0f)
            return;

        if (Time.realtimeSinceStartup < _nextGiftTime)
            return;

        DeliverGift();
    }

    /// <summary>开始探索。仅支持同一时刻一个 Official 探索。</summary>
    public bool StartExplore(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        if (!string.IsNullOrWhiteSpace(_exploringDefinitionId))
            return false;

        ZombieDefinitionSO definition = GetDefinition(definitionId);
        if (definition == null || definition.Category != ZombieCategory.Officer)
            return false;

        var pool = definition.ExploreItemPool;
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"[ZombieExploreService] No explore item pool for {definitionId}");
            return false;
        }

        _exploringDefinitionId = definitionId;
        ScheduleNextGift(definition);
        return true;
    }

    /// <summary>停止探索。若有待领取物品则清空。</summary>
    public void StopExplore(string definitionId)
    {
        if (_exploringDefinitionId != definitionId)
            return;

        _exploringDefinitionId = null;
        _nextGiftTime = -1f;

        if (_pendingDefinitionId == definitionId)
        {
            _pendingItem = null;
            _pendingDefinitionId = null;
        }
    }

    /// <summary>玩家领取礼物。成功后启动下一轮倒计时。</summary>
    public bool ClaimGift()
    {
        if (_pendingItem == null)
            return false;

        _pendingItem = null;
        string exploringId = _pendingDefinitionId;
        _pendingDefinitionId = null;

        OnGiftClaimed?.Invoke();

        if (!string.IsNullOrWhiteSpace(_exploringDefinitionId) && _exploringDefinitionId == exploringId)
        {
            ZombieDefinitionSO definition = GetDefinition(_exploringDefinitionId);
            if (definition != null)
                ScheduleNextGift(definition);
        }

        return true;
    }

    private void ScheduleNextGift(ZombieDefinitionSO definition)
    {
        float interval = definition.ExploreIntervalSeconds;
        if (interval < 1f)
            interval = 60f;
        _nextGiftTime = Time.realtimeSinceStartup + interval;
    }

    private void DeliverGift()
    {
        ZombieDefinitionSO definition = GetDefinition(_exploringDefinitionId);
        if (definition == null)
        {
            _nextGiftTime = -1f;
            return;
        }

        var pool = definition.ExploreItemPool;
        if (pool == null || pool.Count == 0)
        {
            _nextGiftTime = -1f;
            return;
        }

        var validItems = new List<ItemDataSO>();
        foreach (ItemDataSO item in pool)
        {
            if (item != null)
                validItems.Add(item);
        }

        if (validItems.Count == 0)
        {
            _nextGiftTime = -1f;
            return;
        }

        ItemDataSO chosen = validItems[UnityEngine.Random.Range(0, validItems.Count)];
        _pendingItem = chosen;
        _pendingDefinitionId = _exploringDefinitionId;
        _nextGiftTime = -1f;

        OnGiftArrived?.Invoke(chosen, _exploringDefinitionId);
    }

    private ZombieDefinitionSO GetDefinition(string definitionId)
    {
        return _zombieManager != null ? _zombieManager.GetDefinition(definitionId) : null;
    }
}
