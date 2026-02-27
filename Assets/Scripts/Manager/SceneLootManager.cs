using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Assertions.Must; // 必须引用，用于列表操作

public class SceneLootManager : MonoBehaviour
{
    [Header("配置引用")]
    [SerializeField] private SceneLootProfileSO sceneConfig; // 拖入上面的配置文件

    private void Start()
    {
        if (SceneStateManager.Instance != null &&
            SceneStateManager.Instance.HasSceneState(gameObject.scene.name))
        {
            return;
        }
        DistributeLoot();
    }

    [ContextMenu("手动测试分配")] // 允许你在编辑器运行时右键点击测试
    public void DistributeLoot()
    {
        if (sceneConfig == null)
        {
            Debug.LogError("SceneLootManager: 缺少配置 profile！");
            return;
        }

        // 1. 获取场景中所有挖掘点
        List<DiggingTrigger> allSpots = FindObjectsByType<DiggingTrigger>(FindObjectsSortMode.None).Where(trigger => !trigger.isCustomizedPoint).ToList();;
        
        if (allSpots.Count == 0)
        {
            Debug.LogWarning("场景里没有挖掘点 (DiggableSpot)！");
            return;
        }

        Debug.Log($"找到 {allSpots.Count} 个挖掘点，开始分配物品...");

        // 2. 构建待分发的物品清单 (Deck)
        List<ItemDataSO> lootDeck = new List<ItemDataSO>();

        // A. 添加核心产出 (Guaranteed)
        foreach (var entry in sceneConfig.guaranteedLoot)
        {
            if (entry.item != null)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    lootDeck.Add(entry.item);
                }
            }
        }

        // B. 计算是否还有空位需要填充
        // 如果物品数 < 挖掘点数，我们需要用垃圾/填充物把坑填满
        int spotsCount = allSpots.Count;
        int currentLootCount = lootDeck.Count;
        
        if (currentLootCount < spotsCount)
        {
            int neededFillers = spotsCount - currentLootCount;
            
            // 尝试从随机池里填充
            if (sceneConfig.randomFillerPool != null && sceneConfig.randomFillerPool.Count > 0)
            {
                for (int i = 0; i < neededFillers; i++)
                {
                    // 随机抽一个
                    ItemDataSO randomItem = sceneConfig.randomFillerPool[Random.Range(0, sceneConfig.randomFillerPool.Count)];
                    lootDeck.Add(randomItem);
                }
            }
            else if (sceneConfig.fallbackItem != null)
            {
                // 如果没有随机池，用保底物品填满
                for (int i = 0; i < neededFillers; i++)
                {
                    lootDeck.Add(sceneConfig.fallbackItem);
                }
            }
            // 如果连保底都没有，剩下的坑就是空的，不需要操作
        }

        // 3. 洗牌 (Fisher-Yates Shuffle 算法)
        // 这一步确保物品被打散
        int n = lootDeck.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            ItemDataSO value = lootDeck[k];
            lootDeck[k] = lootDeck[n];
            lootDeck[n] = value;
        }

        // 4. 清空所有点位的数据 (防止重复分配)
        foreach (var spot in allSpots)
        {
            // 这里我们需要给 DiggableSpot 加一个 Clear 方法，或者直接 SetContent(空)
            spot.SetContent(new List<ItemDataSO>()); 
        }

        // 5. 发牌 (Distribute)
        // 使用取余算法，确保如果物品比坑多，会循环分配 (一个坑里埋两个东西)
        for (int i = 0; i < lootDeck.Count; i++)
        {
            int spotIndex = i % allSpots.Count; // 核心：循环索引
            DiggingTrigger targetSpot = allSpots[spotIndex];
            
            // 我们需要修改 DiggableSpot 让它支持“添加”而不是“覆盖”
            // 但为了简单，我们假设 SetContent 是覆盖，这里我们先收集好再赋值会比较麻烦
            // 简单做法：直接调用 AddContent
            targetSpot.AddContent(lootDeck[i]);
        }

        Debug.Log($"分配完成！共分配了 {lootDeck.Count} 个物品。");
    }
}