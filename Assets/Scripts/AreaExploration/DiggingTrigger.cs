using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

public class DiggingTrigger : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class DiggingTriggerState
    {
        public bool isDug;
        public List<string> assignedItemNames;
        public bool isActive;
        public bool colliderEnabled;
    }

    [Header("运行时状态 (自动分配)")]
    // 这个列表存储了该点位被分配到的所有物品
    [SerializeField] public bool isCustomizedPoint = false;
    [SerializeField] private List<ItemDataSO> assignedItems = new List<ItemDataSO>();
    
    [Header("配置")]
    [SerializeField] private Sprite dugSprite; // 挖完后的样子
    
    private bool isDug = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col; // 引用碰撞体


    public Canvas buttonCanvas;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("进入挖掘区域");
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

    // --- 供管理器调用的方法 ---

    // 1. 初始化/清空内容
    public void SetContent(List<ItemDataSO> items)
    {
        // if(isCustomizedPoint) 
        // {
        //     return;
        // }
        
        assignedItems = new List<ItemDataSO>(items);
        isDug = false;
    }

    // 2. 添加单个物品
    public void AddContent(ItemDataSO item)
    {
        if (assignedItems == null)
        {
            assignedItems = new List<ItemDataSO>();
        }
        assignedItems.Add(item);
    }

    // --- 玩家交互逻辑 ---

    // 当玩家挖掘时调用此方法
    public void Interact(InventorySO playerInventory)
    {
        if (isDug) return;

        // 如果列表为空，或者里面的物品都是空的
        if (assignedItems == null || assignedItems.Count == 0)
        {
            Debug.Log("这里只有一些松软的泥土 (空)。");
            FinishDigging(); // 直接结束
            return;
        }

        // 如果坑里有东西，取第一个
        ItemDataSO itemToGive = assignedItems[0];

        if(isCustomizedPoint) 
        {
            switch (itemToGive.itemName) 
            {
                case "A Mysterious Book":
                    DialogueLua.SetVariable("hasBook", true);
                    Debug.Log("挖到了书，这里应该变状态");
                    break;
                case "Dead Man's Arms":
                    DialogueLua.SetVariable("hasFirstArm", true);
                    Debug.Log("挖到了胳膊，这里应该变状态");
                    break;
            }

            DialogueSystemTrigger[] allTriggers = GetComponents<DialogueSystemTrigger>();

            foreach (DialogueSystemTrigger trigger in allTriggers)
            {
                if (trigger != null && trigger.enabled)
                {
                    // 向这个 Trigger 发送 "OnUse" 消息，激活 DS 的 On Use 触发器
                    // currentDialogueTrigger.gameObject.SendMessage("OnUse", this.transform, SendMessageOptions.DontRequireReceiver);
                    trigger.OnUse();
                }
            }
            
        }

        // 尝试加入背包
        bool success = playerInventory.AddItem(itemToGive);
        if (success)
        {
            Debug.Log($"挖到了: {itemToGive.itemName}！(剩余物品: {assignedItems.Count - 1})");
            
            // 只有进背包了，才从坑里移除
            assignedItems.RemoveAt(0);

            // 可以在这里播放“叮”的一声或者挖掘特效
        }
        else
        {
            Debug.Log("背包满了！挖不出来！");
            // 背包满了就不移除，玩家清理背包后可以继续来挖
            return; 
        }

        // 再次检查：刚才挖完之后，坑空了吗？
        if (assignedItems.Count == 0)
        {
            FinishDigging();
        }
    }

    // 辅助方法：结束挖掘（挖空了）
    private void FinishDigging()
    {
        isDug = true;
        Debug.Log("这个坑彻底挖空了！");

        // 切换成“坑”的图片
        if (dugSprite != null && spriteRenderer != null) 
        {
            spriteRenderer.sprite = dugSprite;
        }
        
        // 关键：禁用碰撞体
        // 这会自动触发 OnTriggerExit2D，从而让 PlayerInteraction 知道“不能再挖了”
        if (col != null)
        {
            col.enabled = false; 
        }

        this.gameObject.SetActive(false);
    }

    // ── ISaveable ──

    public string CaptureState()
    {
        var names = new List<string>();
        if (assignedItems != null)
        {
            foreach (var item in assignedItems)
                names.Add(item != null ? item.itemName : null);
        }
        var state = new DiggingTriggerState
        {
            isDug = isDug,
            assignedItemNames = names,
            isActive = gameObject.activeSelf,
            colliderEnabled = col != null && col.enabled
        };
        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string stateJson)
    {
        var state = JsonUtility.FromJson<DiggingTriggerState>(stateJson);
        isDug = state.isDug;

        assignedItems = new List<ItemDataSO>();
        if (state.assignedItemNames != null)
        {
            foreach (var name in state.assignedItemNames)
            {
                var item = ItemLookup.Get(name);
                if (item != null) assignedItems.Add(item);
            }
        }

        if (isDug)
        {
            if (dugSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = dugSprite;
        }

        if (col != null)
            col.enabled = state.colliderEnabled;

        gameObject.SetActive(state.isActive);
    }
}
