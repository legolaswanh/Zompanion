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

    [Header("閰嶇疆")]
    [SerializeField] private List<ZombieRecipeSO> allRecipes; // 鎷栧叆鎵€鏈夊彲鑳界殑閰嶆柟
    [SerializeField] private Transform spawnPoint; // 鍍靛案鐢熸垚鐨勪綅缃?

    [Header("璋冪敤鐣岄潰")]
    [SerializeField] private Canvas platformCanvas;
    [SerializeField] private Canvas buttonCanvas;

    [Header("褰撳墠鏀惧叆鐨勯儴浠?(Runtime)")]
    // 杩欓噷绠€鍗曡捣瑙侊紝鐩存帴鐢?ItemDataSO锛屼负绌轰唬琛ㄦ病鏀?
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
        Debug.Log("杩涘叆缁勮鍖哄煙");
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
        if(!platformCanvas.gameObject.activeSelf) 
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

    // 渚?UI 璋冪敤锛氬皾璇曟斁鍏ョ墿鍝?
    // 杩斿洖 true 琛ㄧず鏀惧叆鎴愬姛
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

    // 渚?UI 璋冪敤锛氱偣鍑烩€滅粍瑁呪€濇寜閽?
    public void Assemble()
    {
        // 1. 妫€鏌ユ槸鍚︽墍鏈夐儴浣嶉兘榻愪簡
        if (currentTorso == null || currentArm == null || currentLeg == null)
        {
            Debug.Log("閮ㄤ欢涓嶅叏锛屾棤娉曠粍瑁咃紒");
            return;
        }

        // 2. 閬嶅巻閰嶆柟锛屽鎵惧尮閰嶉」
        ZombieRecipeSO matchedRecipe = null;
        foreach (var recipe in allRecipes)
        {
            if (recipe.IsMatch(currentTorso, currentArm, currentLeg))
            {
                matchedRecipe = recipe;
                break;
            }
        }

        // 3. 鐢熸垚缁撴灉
        if (matchedRecipe != null && matchedRecipe.zombiePrefab != null)
        {
            TryUnlockCodexByRecipe(matchedRecipe);
            Instantiate(matchedRecipe.zombiePrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log("缁勮鎴愬姛锛佺敓鎴愪簡: " + matchedRecipe.zombiePrefab.name);
            
            // 4. 娓呯┖鍙板瓙 (娑堣€楁帀浜?
            ClearPlatform();
        }
        else
        {
            Debug.Log("[AssemblyPlatform] Assemble failed: no matching recipe.");
            // 鍙€夛細鐢熸垚涓€涓粯璁ょ殑鈥滃け璐ュ搧鈥濆兊灏革紝鎴栬€呴€€鍥炴潗鏂?
        }
    }

    public void ClearPlatform()
    {
        // currentHead = null;
        currentTorso = null;
        currentArm = null;
        currentLeg = null;
        // 閫氱煡 UI 鍒锋柊
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
        Debug.Log($"宸蹭粠閫昏緫灞傜Щ闄ら儴浣? {item}");
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

    // 鈹€鈹€ ISaveable 鈹€鈹€

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

