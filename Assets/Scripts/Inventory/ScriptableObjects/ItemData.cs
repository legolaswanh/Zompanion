using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string description;
    public string useInfo;
    public Sprite icon;
    public ItemType itemType;

    [Header("Timeline")]
    [Tooltip("场景中 Timeline GameObject 的名称；使用物品时按此名称查找 PlayableDirector。留空则不播放 Timeline。")]
    public string timelineObjectName;

    [Header("Prefab")]
    public GameObject worldPrefab;
}
