using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAssemblySlot : MonoBehaviour, IDropHandler
{
    [Header("这个格子接收的部位类型")]
    public ItemType acceptableType; 
    
    [Header("引用")]
    public AssemblyPlatform platform;

    [Header("图标")]
    [Tooltip("与背包格子共用同一个 ItemIcon 预制体")]
    [SerializeField] private GameObject itemIconPrefab;


    private void OnEnable()
    {
        if (platform != null)
        {
            platform.OnAssemblyCleared += ClearSlotUI;
            platform.OnAssemblyRestored += RefreshSlotUI;
            RefreshSlotUI();
        }
    }

    private void OnDisable()
    {
        if (platform != null)
        {
            platform.OnAssemblyCleared -= ClearSlotUI;
            platform.OnAssemblyRestored -= RefreshSlotUI;
        }
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
                GetComponent<Image>().enabled = false;
                Debug.Log($"已放入部位: {draggedItemUI.itemData.itemName}");
            }
        }
    }

    private void ClearSlotUI()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        GetComponent<Image>().enabled = true;
    }

    /// <summary>
    /// 状态恢复后刷新图标：根据 platform 数据重建 ItemIcon。
    /// 先在根 Canvas 下实例化，再 SetParent(worldPositionStays=true) 到本槽位，
    /// 复现拖拽放入时的 transform 行为（自动补偿父级缩放、缓存正确的 Canvas 引用）。
    /// </summary>
    private void RefreshSlotUI()
    {
        ClearSlotUI();

        if (platform == null) return;
        ItemDataSO item = platform.GetPart(acceptableType);
        if (item == null) return;

        if (itemIconPrefab == null)
        {
            Debug.LogWarning($"[UIAssemblySlot] {gameObject.name} 未配置 itemIconPrefab，无法恢复图标");
            return;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Transform spawnParent = rootCanvas != null ? rootCanvas.transform : transform;

        GameObject obj = Instantiate(itemIconPrefab, spawnParent);
        obj.transform.SetParent(transform);
        obj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        var itemUI = obj.GetComponent<ItemUI>();
        if (itemUI != null)
            itemUI.SetItem(item);

        GetComponent<Image>().enabled = false;
    }
}