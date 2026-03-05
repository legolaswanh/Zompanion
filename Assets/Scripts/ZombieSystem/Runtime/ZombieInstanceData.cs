using System;
using UnityEngine;
using Zompanion.ZombieSystem;

[Serializable]
public class ZombieInstanceData
{
    public int instanceId;
    public string definitionId;
    public string displayName;
    public ZombieState state = ZombieState.Idle;
    public int followOrder = -1;
    public bool modifierApplied;
    public ZombieModifierType appliedModifierType = ZombieModifierType.None;
    public ZombieModifierApplyMode appliedModifierApplyMode = ZombieModifierApplyMode.OnSpawn;
    public float appliedModifierValue;
    public string unlockStoryId;
    public bool hasHomeAnchor;
    public string homeSceneName;
    public Vector3 homePosition;
    public Vector3 homeEulerAngles;
    public bool pendingReturnToHome;

    public ZombieInstanceData(int id, string defId, string name)
    {
        instanceId = id;
        definitionId = defId;
        displayName = name;
    }
}
