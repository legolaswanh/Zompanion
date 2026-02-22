using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform originalParent;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake() 
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData) 
    {
        originalParent = transform.parent;

        // 如果是从合成槽位拖出来的
        UIAssemblySlot sourceSlot = transform.parent.GetComponent<UIAssemblySlot>();
        if (sourceSlot != null)
        {
            // 清理逻辑层对应的部位
            sourceSlot.platform.RemovePart(sourceSlot.acceptableType);
        }
        
        // 拖拽时让图标显示在最上层
        transform.SetParent(canvas.transform); 
        // 降低透明度并允许射线穿透，否则 EndDrag 无法检测到底下的格子
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) 
    {
        // 让图标跟随鼠标移动
        if(canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData) 
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 如果没有落在合法的 Slot 上，就回到原位
        if (transform.parent == canvas.transform) 
        {
            ReturnToOriginalSlot();
        }
    }

    public void ReturnToOriginalSlot() 
    {
        // 这里的逻辑需要分情况：
        
        // 情况A：如果老家是合成台 (AssemblySlot)，让它回去，并且恢复逻辑数据
        UIAssemblySlot assemblySlot = originalParent.GetComponent<UIAssemblySlot>();
        if (assemblySlot != null)
        {
            // 恢复逻辑数据
            ItemUI itemUI = GetComponent<ItemUI>();
            if(itemUI != null) assemblySlot.platform.InsertPart(itemUI.itemData);
            
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        // 情况B：如果老家是背包 (InventorySlotUI)，不需要做任何事，因为背包数据并没有在 OnBeginDrag 时被清除。
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}