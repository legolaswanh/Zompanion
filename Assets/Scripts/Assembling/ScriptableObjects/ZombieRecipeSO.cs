using UnityEngine;

[CreateAssetMenu(fileName = "New Zombie Recipe", menuName = "Assembling/Zombie Recipe")]
public class ZombieRecipeSO : ScriptableObject
{
    [Header("所需部件 (Ingredients)")]
    // public ItemDataSO headItem;
    public ItemDataSO torsoItem;
    public ItemDataSO armItem;
    public ItemDataSO legItem;

    [Header("合成结果 (Result)")]
    public GameObject zombiePrefab; // 生成的僵尸预制体
    
    // 校验方法：检查传入的四个物品是否匹配这个配方
    public bool IsMatch(ItemDataSO t, ItemDataSO a, ItemDataSO l)
    {
        return t == torsoItem && a == armItem && l == legItem;
    }
}