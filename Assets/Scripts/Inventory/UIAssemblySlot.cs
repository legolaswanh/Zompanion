using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssemblySlot : MonoBehaviour, IDropHandler
{
    [Header("这个格子接收的部位类型")]
    public ItemType acceptableType; 
    
    [Header("引用")]
    public AssemblyPlatform platform;


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
        if (platform == null) return;

        // 1. 获取拖拽中的物体
        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) return;

        // 2. 获取物品数据
        ItemUI draggedItemUI = draggedObj.GetComponent<ItemUI>();
        if (draggedItemUI == null || draggedItemUI.itemData == null) return;
        
        if (draggedItemUI.itemData.itemType == acceptableType)
        {
            // 1. 尝试将新数据插入逻辑层
            if (platform.InsertPart(draggedItemUI.itemData))
            {
                // 获取拖拽物品的来源格子 (以便把旧物品塞回去，或者清空来源)
                UIDragHandler dragHandler = draggedObj.GetComponent<UIDragHandler>();
                InventorySlotUI sourceInventorySlot = dragHandler != null && dragHandler.originalParent != null 
                    ? dragHandler.originalParent.GetComponent<InventorySlotUI>() 
                    : null;

                // 2. 处理当前格子里可能存在的【旧物品】
                ItemUI oldItemUI = GetComponentInChildren<ItemUI>();
                if (oldItemUI != null)
                {
                    // --- 发生替换（Swap） ---
                    if (sourceInventorySlot != null)
                    {
                        // 把旧物品的数据，写回到新物品空出来的那个背包格子里
                        // 这个操作会触发背包 UI 自动生成旧物品的图标
                        sourceInventorySlot.inventoryData.SetItemAt(sourceInventorySlot.slotIndex, oldItemUI.itemData);
                    }
                    
                    // 逻辑和数据已经转移，销毁合成格里旧物品的视觉残留
                    Destroy(oldItemUI.gameObject);
                }
                else
                {
                    // --- 没有发生替换，单纯放入 ---
                    if (sourceInventorySlot != null)
                    {
                        // 因为是从背包拿出来的，把背包原来的格子数据设为空
                        sourceInventorySlot.inventoryData.SetItemAt(sourceInventorySlot.slotIndex, null);
                    }
                }

                // 视觉处理：把图标留在合成格里
                draggedObj.transform.SetParent(this.transform);
                draggedObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                Debug.Log($"已放入部位: {draggedItemUI.itemData.itemName}");
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