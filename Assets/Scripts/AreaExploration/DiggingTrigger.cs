using UnityEngine;
using System.Collections.Generic;

public class DiggingTrigger : MonoBehaviour
{
    [Header("运行时状态 (自动分配)")]
    // 这个列表存储了该点位被分配到的所有物品
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

        // 尝试加入背包
        bool success = playerInventory.AddItem(itemToGive, 1);
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
    }
}
