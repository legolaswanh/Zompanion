namespace Zompanion.ZombieSystem
{
    public enum ZombieType
    {
        Normal = 0,
        Special = 1
    }

    public enum ZombieCategory
    {
        Unknown = 0,
        Monk = 1,
        Officer = 2,
        Worker = 3
    }

    public enum ZombieBuffType
    {
        None = 0,
        BackpackCapacity = 1,
        DiggingLootBonus = 2
    }

    public enum ZombieState
    {
        Idle = 0,
        Following = 1,
        Working = 2
    }
}

