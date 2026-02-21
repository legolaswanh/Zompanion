using UnityEngine;

public static class ZombieRuntimeModifiers
{
    public static float DiggingLootBonusPercent { get; private set; }

    public static void AddDiggingLootBonus(float value)
    {
        DiggingLootBonusPercent += value;
    }

    public static void Clear()
    {
        DiggingLootBonusPercent = 0f;
    }

    public static float ApplyDiggingBonus(float baseChance)
    {
        float boosted = baseChance * (1f + DiggingLootBonusPercent);
        return Mathf.Clamp01(boosted);
    }
}

