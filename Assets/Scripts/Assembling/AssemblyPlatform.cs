using System.Collections.Generic;
using UnityEngine;

public class AssemblyPlatform : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private List<ZombieRecipeSO> allRecipes; // 拖入所有可能的配方
    [SerializeField] private Transform spawnPoint; // 僵尸生成的位置

    [Header("当前放入的部件 (Runtime)")]
    // 这里简单起见，直接用 ItemDataSO，为空代表没放
    // public ItemDataSO currentHead;
    public ItemDataSO currentTorso;
    public ItemDataSO currentArm;
    public ItemDataSO currentLeg;

    // 供 UI 调用：尝试放入物品
    // 返回 true 表示放入成功
    public bool InsertPart(ItemDataSO item)
    {
        switch (item.itemType)
        {
            // case ItemType.Head:
            //     currentHead = item;
            //     return true;
            case ItemType.Torso:
                currentTorso = item;
                return true;
            case ItemType.Arm:
                currentArm = item;
                return true;
            case ItemType.Leg:
                currentLeg = item;
                return true;
            default:
                Debug.Log("这个东西不能用来组装僵尸！");
                return false;
        }
    }

    // 供 UI 调用：点击“组装”按钮
    public void Assemble()
    {
        // 1. 检查是否所有部位都齐了
        if (currentTorso == null || currentArm == null || currentLeg == null)
        {
            Debug.Log("部件不全，无法组装！");
            return;
        }

        // 2. 遍历配方，寻找匹配项
        ZombieRecipeSO matchedRecipe = null;
        foreach (var recipe in allRecipes)
        {
            if (recipe.IsMatch(currentTorso, currentArm, currentLeg))
            {
                matchedRecipe = recipe;
                break;
            }
        }

        // 3. 生成结果
        if (matchedRecipe != null && matchedRecipe.zombiePrefab != null)
        {
            Instantiate(matchedRecipe.zombiePrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log("组装成功！生成了: " + matchedRecipe.zombiePrefab.name);
            
            // 4. 清空台子 (消耗掉了)
            ClearPlatform();
        }
        else
        {
            Debug.Log("组装失败：没有匹配的配方（可能是个缝合怪？）");
            // 可选：生成一个默认的“失败品”僵尸，或者退回材料
        }
    }

    public void ClearPlatform()
    {
        // currentHead = null;
        currentTorso = null;
        currentArm = null;
        currentLeg = null;
        // 记得通知 UI 刷新
    }
}