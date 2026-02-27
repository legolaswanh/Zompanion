using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跨场景单例：在内存中维护每个场景的状态快照，场景加载时自动恢复。
/// 同时提供序列化/反序列化接口供 SaveSystem 存档到磁盘。
/// </summary>
public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    readonly Dictionary<string, List<EntitySaveData>> _sceneStates = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreSceneState(scene.name);
    }

    /// <summary>是否有指定场景的已保存状态。</summary>
    public bool HasSceneState(string sceneName)
    {
        return _sceneStates.ContainsKey(sceneName) && _sceneStates[sceneName].Count > 0;
    }

    /// <summary>
    /// 捕获当前活动场景中所有 SaveableEntity 的状态，存入内存字典。
    /// 在场景切换/存档前调用。
    /// </summary>
    public void CaptureCurrentSceneState()
    {
        var scene = SceneManager.GetActiveScene();
        CaptureSceneState(scene.name);
    }

    void CaptureSceneState(string sceneName)
    {
        var entities = FindSaveableEntitiesInScene(sceneName);
        if (entities.Count == 0) return;

        var dataList = new List<EntitySaveData>();
        foreach (var entity in entities)
        {
            var stateMap = entity.CaptureAll();
            foreach (var kvp in stateMap)
            {
                dataList.Add(new EntitySaveData
                {
                    entityId = entity.UniqueId,
                    componentType = kvp.Key,
                    stateJson = kvp.Value
                });
            }
        }

        _sceneStates[sceneName] = dataList;
    }

    void RestoreSceneState(string sceneName)
    {
        if (!_sceneStates.TryGetValue(sceneName, out var dataList)) return;
        if (dataList.Count == 0) return;

        var entities = FindSaveableEntitiesInScene(sceneName);
        var entityMap = new Dictionary<string, SaveableEntity>();
        foreach (var e in entities)
        {
            if (!string.IsNullOrEmpty(e.UniqueId))
                entityMap[e.UniqueId] = e;
        }

        var grouped = new Dictionary<string, Dictionary<string, string>>();
        foreach (var data in dataList)
        {
            if (!grouped.ContainsKey(data.entityId))
                grouped[data.entityId] = new Dictionary<string, string>();
            grouped[data.entityId][data.componentType] = data.stateJson;
        }

        foreach (var kvp in grouped)
        {
            if (entityMap.TryGetValue(kvp.Key, out var entity))
                entity.RestoreAll(kvp.Value);
        }
    }

    List<SaveableEntity> FindSaveableEntitiesInScene(string sceneName)
    {
        var result = new List<SaveableEntity>();
#if UNITY_2023_1_OR_NEWER
        var all = FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<SaveableEntity>(true);
#endif
        foreach (var se in all)
        {
            if (se.gameObject.scene.name == sceneName)
                result.Add(se);
        }
        return result;
    }

    /// <summary>新游戏时清空所有已缓存的场景状态。</summary>
    public void ClearAllStates()
    {
        _sceneStates.Clear();
    }

    // ── 序列化/反序列化（供 SaveSystem 存档到磁盘）──

    /// <summary>将所有场景状态序列化为 JSON 字符串。</summary>
    public string SerializeAllToJson()
    {
        var wrapper = new AllScenesSaveData();
        foreach (var kvp in _sceneStates)
        {
            wrapper.scenes.Add(new SceneSaveData
            {
                sceneName = kvp.Key,
                entities = kvp.Value
            });
        }
        return JsonUtility.ToJson(wrapper);
    }

    /// <summary>从 JSON 字符串恢复所有场景状态到内存字典。</summary>
    public void DeserializeAllFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var wrapper = JsonUtility.FromJson<AllScenesSaveData>(json);
        if (wrapper?.scenes == null) return;
        _sceneStates.Clear();
        foreach (var scene in wrapper.scenes)
            _sceneStates[scene.sceneName] = scene.entities ?? new List<EntitySaveData>();
    }

    // ── 序列化数据类 ──

    [Serializable]
    public class EntitySaveData
    {
        public string entityId;
        public string componentType;
        public string stateJson;
    }

    [Serializable]
    public class SceneSaveData
    {
        public string sceneName;
        public List<EntitySaveData> entities = new();
    }

    [Serializable]
    public class AllScenesSaveData
    {
        public List<SceneSaveData> scenes = new();
    }
}
