using System;
using System.Collections.Generic;

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
}

