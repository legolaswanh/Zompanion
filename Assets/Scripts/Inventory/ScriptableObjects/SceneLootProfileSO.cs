using System.Collections.Generic;
using UnityEngine;

// 1. 定义单条配置数据：什么东西？有几个？
[System.Serializable]
public class LootDefinition
{
    [Tooltip("要生成的物品")]
    public ItemDataSO item;
    
    [Tooltip("场景中必定生成的总数量")]
    [Min(1)] public int count = 1;

    [Tooltip("是否为剧情关键物品（可选：用于特殊逻辑处理）")]
    public bool isKeyItem = false;
}

[CreateAssetMenu(fileName = "New Scene Profile", menuName = "Inventory/Scene Loot Profile")]
public class SceneLootProfileSO : ScriptableObject
{
    [Header("1. 核心产出 (必掉)")]
    [Tooltip("无论如何都会生成的物品，例如：关键尸块、任务道具")]
    public List<LootDefinition> guaranteedLoot;

    [Header("2. 随机填充池 (垃圾/通用)")]
    [Tooltip("当核心产出分发完后，剩余的空坑会从这里随机抽取物品填满")]
    public List<ItemDataSO> randomFillerPool;

    [Header("3. 保底物品 (兜底)")]
    [Tooltip("如果填充池也没东西了，剩下的坑填什么？(例如泥土块)。留空则表示什么都没有")]
    public ItemDataSO fallbackItem;
    
    [Header("调试信息")]
    [TextArea] public string developerNotes;
}