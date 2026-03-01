using System;
using System.Collections.Generic;
using UnityEngine;

public class AssemblyPlatform : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class AssemblyPlatformState
    {
        public string currentTorsoName;
        public string currentArmName;
        public string currentLegName;
    }

    [Header("配置")]
    [SerializeField] private List<ZombieRecipeSO> allRecipes; // 拖入所有可能的配方
    [SerializeField] private Transform spawnPoint; // 僵尸生成的位置

    [Header("调用界面")]
    [SerializeField] private Canvas platformCanvas;
    [SerializeField] private Canvas buttonCanvas;

    [Header("当前放入的部件 (Runtime)")]
    // 这里简单起见，直接用 ItemDataSO，为空代表没放
    // public ItemDataSO currentHead;
    public ItemDataSO currentTorso;
    public ItemDataSO currentArm;
    public ItemDataSO currentLeg;

    public event Action OnAssemblyCleared;
    public event Action OnAssemblyRestored;
    private BoxCollider2D platformCollider;

    void Awake()
    {
        platformCollider = GetComponent<BoxCollider2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("进入组装区域");
        if (collision != null && collision.CompareTag("Player"))
        {
            buttonCanvas.gameObject.SetActive(true);
            PlayerInteraction.Instance.SetCurrentTrigger(this.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        buttonCanvas.gameObject.SetActive(false);
        if (collision.CompareTag("Player"))
        {
            buttonCanvas.gameObject.SetActive(false);
            PlayerInteraction.Instance.ClearCurrentTrigger(this.gameObject);
        }
    }

    public void OpenPlatFormUI()
    {
        if (!platformCanvas.gameObject.activeSelf)
        {
            platformCanvas.gameObject.SetActive(true);
            PlayerMovement.Instance.DisableMove();
        }
        else
        {
            platformCanvas.gameObject.SetActive(false);
            PlayerMovement.Instance.EnableMove();
        }
    }

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
                Debug.Log("[AssemblyPlatform] This item cannot be used for assembling.");
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
            TryUnlockCodexByRecipe(matchedRecipe);
            Instantiate(matchedRecipe.zombiePrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log("组装成功！生成了: " + matchedRecipe.zombiePrefab.name);

            // 4. 清空台子（消耗掉）
            ClearPlatform();
        }
        else
        {
            Debug.Log("[AssemblyPlatform] Assemble failed: no matching recipe.");
            // 可选：生成一个默认的“失败品”僵尸，或者退回材料
        }
    }

    public void ClearPlatform()
    {
        // currentHead = null;
        currentTorso = null;
        currentArm = null;
        currentLeg = null;
        // 通知 UI 刷新
        OnAssemblyCleared?.Invoke();
        Debug.Log("[AssemblyPlatform] Cleared current parts and notified UI.");
    }

    public void RemovePart(ItemType item)
    {
        switch (item)
        {
            case ItemType.Torso:
                currentTorso = null;
                break;
            case ItemType.Arm:
                currentArm = null;
                break;
            case ItemType.Leg:
                currentLeg = null;
                break;
        }
        Debug.Log($"已从逻辑层移除部位: {item}");
    }

    // Unlock codex entry when a recipe assembles successfully.
    private bool TryUnlockCodexByRecipe(ZombieRecipeSO recipe)
    {
        if (recipe == null || recipe.resultDefinition == null)
            return false;

        if (ZombieManager.Instance == null)
            return false;

        return ZombieManager.Instance.UnlockZombieCodex(recipe.resultDefinition.DefinitionId);
    }

    public ItemDataSO GetPart(ItemType type)
    {
        return type switch
        {
            ItemType.Torso => currentTorso,
            ItemType.Arm => currentArm,
            ItemType.Leg => currentLeg,
            _ => null
        };
    }

    // —— ISaveable ——

    public string CaptureState()
    {
        var state = new AssemblyPlatformState
        {
            currentTorsoName = currentTorso != null ? currentTorso.itemName : null,
            currentArmName = currentArm != null ? currentArm.itemName : null,
            currentLegName = currentLeg != null ? currentLeg.itemName : null
        };
        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string stateJson)
    {
        var state = JsonUtility.FromJson<AssemblyPlatformState>(stateJson);
        currentTorso = ItemLookup.Get(state.currentTorsoName);
        currentArm = ItemLookup.Get(state.currentArmName);
        currentLeg = ItemLookup.Get(state.currentLegName);
        OnAssemblyRestored?.Invoke();
    }
}