namespace Zompanion.ZombieSystem
{
    public enum ZombieCategory
    {
        Unknown = 0,
        Monk = 1,
        Officer = 2,
        Worker = 3
    }

    public enum ZombieModifierType
    {
        None = 0,
        BackpackCapacity = 1,
        DiggingLootBonus = 2
    }

    public enum ZombieModifierApplyMode
    {
        OnSpawn = 0,
        WhileFollowing = 1
    }

    public enum ZombieState
    {
        Idle = 0,
        Following = 1,
        Working = 2
    }
}
