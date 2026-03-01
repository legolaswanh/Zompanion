using UnityEngine;

[CreateAssetMenu(fileName = "New Zombie Recipe", menuName = "Assembling/Zombie Recipe")]
public class ZombieRecipeSO : ScriptableObject
{
    [Header("Ingredients")]
    public ItemDataSO torsoItem;
    public ItemDataSO armItem;
    public ItemDataSO legItem;

    [Header("Result")]
    public GameObject zombiePrefab;
    public ZombieDefinitionSO resultDefinition;

    public bool IsMatch(ItemDataSO torso, ItemDataSO arm, ItemDataSO leg)
    {
        return torso == torsoItem && arm == armItem && leg == legItem;
    }
}
