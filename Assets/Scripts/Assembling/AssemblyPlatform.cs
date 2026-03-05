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
    [SerializeField] private List<ZombieRecipeSO> allRecipes;
    [SerializeField] private Transform spawnPoint;

    [Header("调用界面")]
    [SerializeField] private Canvas platformCanvas;

    [Header("小游戏")]
    [SerializeField] private SummonCanvas summonCanvasPrefab;

    [Header("当前放入的部件 (Runtime)")]
    public ItemDataSO currentTorso;
    public ItemDataSO currentArm;
    public ItemDataSO currentLeg;

    public event Action OnAssemblyCleared;
    public event Action OnAssemblyRestored;
    private BoxCollider2D platformCollider;
    private ZombieRecipeSO _pendingRecipe;
    private SummonCanvas _summonCanvasInstance;
    private InteractButtonCanvasSpawner _spawner;

    void Awake()
    {
        platformCollider = GetComponent<BoxCollider2D>();
        _spawner = GetComponent<InteractButtonCanvasSpawner>();
    }

    void OnEnable() { }

    void OnDisable()
    {
        if (_summonCanvasInstance != null)
        {
            _summonCanvasInstance.OnMiniGameCompleted -= HandleMiniGameResult;
            Destroy(_summonCanvasInstance.gameObject);
            _summonCanvasInstance = null;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("进入组装区域");
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Show();
            PlayerInteraction.Instance.SetCurrentTrigger(this.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            _spawner?.Hide();
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

    public bool InsertPart(ItemDataSO item)
    {
        switch (item.itemType)
        {
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

    public void Assemble()
    {
        if (currentTorso == null || currentArm == null || currentLeg == null)
        {
            Debug.Log("部件不全，无法组装！");
            return;
        }

        ZombieRecipeSO matchedRecipe = null;
        foreach (var recipe in allRecipes)
        {
            if (recipe.IsMatch(currentTorso, currentArm, currentLeg))
            {
                matchedRecipe = recipe;
                break;
            }
        }

        if (matchedRecipe != null && matchedRecipe.resultDefinition != null)
        {
            _pendingRecipe = matchedRecipe;

            if (summonCanvasPrefab != null)
            {
                platformCanvas.gameObject.SetActive(false);

                var go = Instantiate(summonCanvasPrefab.gameObject);
                _summonCanvasInstance = go.GetComponent<SummonCanvas>();
                _summonCanvasInstance.OnMiniGameCompleted += HandleMiniGameResult;
                _summonCanvasInstance.StartDraw();
            }
            else
            {
                SpawnZombie(_pendingRecipe);
            }
        }
        else
        {
            Debug.Log("[AssemblyPlatform] Assemble failed: no matching recipe or missing resultDefinition.");
        }
    }

    private void HandleMiniGameResult(bool success)
    {
        if (_summonCanvasInstance != null)
        {
            _summonCanvasInstance.OnMiniGameCompleted -= HandleMiniGameResult;
            Destroy(_summonCanvasInstance.gameObject);
            _summonCanvasInstance = null;
        }

        PlayerMovement.Instance.EnableMove();

        if (success && _pendingRecipe != null)
        {
            SpawnZombie(_pendingRecipe);
        }
        else
        {
            Debug.Log("[AssemblyPlatform] 小游戏未通过，组装取消");
            _pendingRecipe = null;
        }
    }

    private void SpawnZombie(ZombieRecipeSO recipe)
    {
        if (recipe == null || recipe.resultDefinition == null)
        {
            Debug.LogWarning("[AssemblyPlatform] Spawn failed: recipe or resultDefinition is null.");
            _pendingRecipe = null;
            return;
        }

        if (ZombieManager.Instance == null)
        {
            Debug.LogError("[AssemblyPlatform] ZombieManager not found. Cannot spawn assembled zombie.");
            _pendingRecipe = null;
            return;
        }

        TryUnlockCodexByRecipe(recipe);
        ZombieInstanceData data = ZombieManager.Instance.SpawnZombie(
            recipe.resultDefinition,
            autoFollow: false,
            ignoreCodexUnlock: true);

        if (data == null)
        {
            Debug.LogWarning($"[AssemblyPlatform] Spawn failed for '{recipe.resultDefinition.DefinitionId}'.");
            _pendingRecipe = null;
            return;
        }

        Debug.Log($"[AssemblyPlatform] Assemble success, zombie ready: {recipe.resultDefinition.DefinitionId} (instanceId: {data.instanceId}).");
        _pendingRecipe = null;
        ClearPlatform();
    }

    public void ClearPlatform()
    {
        currentTorso = null;
        currentArm = null;
        currentLeg = null;
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
