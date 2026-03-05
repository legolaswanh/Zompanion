using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform originalParent;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        isDragging = true;

        UIAssemblySlot sourceAssembly = originalParent != null ? originalParent.GetComponent<UIAssemblySlot>() : null;
        if (sourceAssembly != null)
        {
            sourceAssembly.platform.RemovePart(sourceAssembly.acceptableType);
            Image slotImage = sourceAssembly.transform.GetComponent<Image>();
            if (slotImage != null)
                slotImage.enabled = true;
        }

        if (canvas != null)
            transform.SetParent(canvas.transform);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        InventorySlotUI sourceInventorySlot = originalParent != null ? originalParent.GetComponent<InventorySlotUI>() : null;
        ItemUI itemUI = GetComponent<ItemUI>();
        Sprite icon = itemUI != null && itemUI.itemData != null ? itemUI.itemData.icon : null;
        InventoryDragContext.Begin(sourceInventorySlot, icon);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || rectTransform == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (canvas != null && transform.parent == canvas.transform)
            ReturnToOriginalSlot();

        isDragging = false;
        InventoryDragContext.Clear();
    }

    public void ReturnToOriginalSlot()
    {
        if (originalParent == null)
            return;

        UIAssemblySlot assemblySlot = originalParent.GetComponent<UIAssemblySlot>();
        if (assemblySlot != null)
        {
            ItemUI itemUI = GetComponent<ItemUI>();
            if (itemUI != null)
                assemblySlot.platform.InsertPart(itemUI.itemData);

            transform.SetParent(originalParent);
            if (rectTransform != null)
                rectTransform.anchoredPosition = Vector2.zero;

            Image slotImage = assemblySlot.transform.GetComponent<Image>();
            if (slotImage != null)
                slotImage.enabled = false;
            return;
        }

        transform.SetParent(originalParent);
        if (rectTransform != null)
            rectTransform.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        if (!isDragging)
            return;

        isDragging = false;
        InventoryDragContext.Clear();
    }
}
