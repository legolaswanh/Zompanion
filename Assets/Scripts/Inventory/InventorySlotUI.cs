using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    [Header("Config")]
    public int slotIndex; // 这一格在背包里的索引
    public InventorySO inventoryData; // 引用 SO 方便直接修改数据
    
    [Header("Prefabs")]
    [SerializeField] private GameObject itemIconPrefab;


    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null) return;

        // 如果这个格子里已经有东西了，就不允许放下
        ItemUI draggedItem = draggedObj.GetComponent<ItemUI>();
        if (draggedItem == null || draggedItem.itemData == null) return;
        
        // 只有当这个格子是空的时候，才允许放进来
        // 检查当前格子数据是否为空
        if (inventoryData.slots[slotIndex].IsEmpty)
        {
            // 1. 修改背包数据（这会自动触发 UpdateSlotDisplay，生成新的图标）
            inventoryData.SetItemAt(slotIndex, draggedItem.itemData);

            // 2. 销毁拖拽中的这个临时物体（因为 UpdateDisplay 会生成一个新的）
            Destroy(draggedObj);

            // 3. 【重要】如果这个物品是从 AssemblySlot 拖出来的，需要清理 AssemblySlot 的逻辑
            // 这部分通常由 DragHandler 的 OnBeginDrag 处理了，或者我们可以检查来源
            UIAssemblySlot sourceAssembly = draggedObj.GetComponent<UIDragHandler>()?.originalParent.GetComponent<UIAssemblySlot>();
            if (sourceAssembly != null)
            {
                sourceAssembly.platform.RemovePart(sourceAssembly.acceptableType);
            }
        }
    }

    // 刷新格子的显示
    public void UpdateSlotDisplay(InventorySlot slot, int index)
    {
        slotIndex = index;

        // 1. 检查当前格子里有没有 ItemIcon
        ItemUI currentItemUI = GetComponentInChildren<ItemUI>();

        // 2. 如果数据为空，但有图标 -> 销毁图标
        if (slot.IsEmpty)
        {
            if (currentItemUI != null) Destroy(currentItemUI.gameObject);
            return;
        }

        // 3. 如果数据不为空
        if (!slot.IsEmpty)
        {
            // 如果还没有图标，生成一个
            if (currentItemUI == null)
            {
                GameObject obj = Instantiate(itemIconPrefab, transform);
                currentItemUI = obj.GetComponent<ItemUI>();
                // 归零坐标
                obj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }

            // 更新图标显示
            currentItemUI.SetItem(slot.itemData);
        }
    }
}
