using System;
using Zompanion.ZombieSystem;

[Serializable]
public class ZombieInstanceData
{
    public int instanceId;
    public string definitionId;
    public string displayName;
    public ZombieState state = ZombieState.Idle;
    public int followOrder = -1;
    public bool buffApplied;
    public ZombieBuffType appliedBuffType = ZombieBuffType.None;
    public float appliedBuffValue;
    public string unlockStoryId;

    public ZombieInstanceData(int id, string defId, string name)
    {
        instanceId = id;
        definitionId = defId;
        displayName = name;
    }
}

