using System;
using System.Collections.Generic;

[Serializable]
public class ZombieCodexSaveData
{
    public List<string> unlockedZombieIds = new List<string>();
    public List<string> unlockedStoryIds = new List<string>();
}

public class ZombieCodexService
{
    private readonly HashSet<string> _unlockedZombieDefinitionIds = new HashSet<string>();
    private readonly HashSet<string> _unlockedStoryIds = new HashSet<string>();

    public event Action OnCodexUpdated;

    public bool UnlockZombie(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return false;
        bool added = _unlockedZombieDefinitionIds.Add(definitionId);
        if (added) OnCodexUpdated?.Invoke();
        return added;
    }

    public bool UnlockStory(string storyId)
    {
        if (string.IsNullOrWhiteSpace(storyId)) return false;
        bool added = _unlockedStoryIds.Add(storyId);
        if (added) OnCodexUpdated?.Invoke();
        return added;
    }

    public bool IsZombieUnlocked(string definitionId)
    {
        return !string.IsNullOrWhiteSpace(definitionId) && _unlockedZombieDefinitionIds.Contains(definitionId);
    }

    public bool IsStoryUnlocked(string storyId)
    {
        return !string.IsNullOrWhiteSpace(storyId) && _unlockedStoryIds.Contains(storyId);
    }

    /// <summary>导出当前图鉴状态供存档使用。</summary>
    public ZombieCodexSaveData ExportState()
    {
        return new ZombieCodexSaveData
        {
            unlockedZombieIds = new List<string>(_unlockedZombieDefinitionIds),
            unlockedStoryIds = new List<string>(_unlockedStoryIds)
        };
    }

    /// <summary>从存档恢复图鉴状态。</summary>
    public void ImportState(ZombieCodexSaveData data)
    {
        _unlockedZombieDefinitionIds.Clear();
        _unlockedStoryIds.Clear();

        if (data?.unlockedZombieIds != null)
        {
            foreach (string id in data.unlockedZombieIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _unlockedZombieDefinitionIds.Add(id);
            }
        }

        if (data?.unlockedStoryIds != null)
        {
            foreach (string id in data.unlockedStoryIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _unlockedStoryIds.Add(id);
            }
        }

        OnCodexUpdated?.Invoke();
    }
}

