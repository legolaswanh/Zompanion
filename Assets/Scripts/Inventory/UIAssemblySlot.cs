using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssemblySlot : MonoBehaviour, IDropHandler
{
    [Header("这个格子接收的部位类型")]
    public ItemType acceptableType; 
    
    [Header("引用")]
    public AssemblyPlatform platform;

    [Header("背包容器，用于退回物品")]
    public Transform inventoryContent;

    void Start()
    {
        if (inventoryContent == null)
        {
            GameObject inventoryUIContainer = GameObject.FindWithTag("InventoryContent");
            if (inventoryUIContainer != null) inventoryContent = inventoryUIContainer.transform;
        }
    }

    private void OnEnable()
    {
        // 订阅事件
        if (platform != null) platform.OnAssemblyCleared += ClearSlotUI;
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        if (platform != null) platform.OnAssemblyCleared -= ClearSlotUI;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("检测到掉落！");
        if (platform == null) return;

        // 1. 获取拖拽中的物体
        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) return;

        // 2. 获取物品数据
        ItemUI draggedItemUI = draggedObj.GetComponent<ItemUI>();
        
        if (draggedItemUI != null && draggedItemUI.itemData != null)
        {
            // 3. 校验类型：只有类型匹配才允许放入
            if (draggedItemUI.itemData.itemType == acceptableType)
            {
                // 调用 InsertPart 方法
                if (platform.InsertPart(draggedItemUI.itemData))
                {
                    // 检查槽位是否已有物品
                    if (transform.childCount > 0)
                    {
                        // 找到当前在合成格里的旧物品
                        UIDragHandler oldItemDrag = GetComponentInChildren<UIDragHandler>();
                        if (oldItemDrag != null)
                        {
                            // 让旧物品回到它原本在背包里的格子
                            oldItemDrag.ReturnToOriginalSlot();
                        }
                    }

                    // 视觉处理：把图标留在合成格里
                    draggedObj.transform.SetParent(this.transform);
                    draggedObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    Debug.Log($"已放入部位: {draggedItemUI.itemData.itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"类型不匹配！这里需要 {acceptableType}");
            }
        }
    }

    private void ClearSlotUI()
    {
        // 遍历并销毁这个格子里所有的子物体（即拖进来的 ItemIcon）
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}