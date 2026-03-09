using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float DoubleClickThreshold = 0.33f;

    [Header("Config")]
    public int slotIndex;
    public IStorageData inventoryData;

    [Header("Prefabs")]
    [SerializeField] private GameObject itemIconPrefab;
    [Tooltip("物品图标占格子大小的比例，1=100%，0.85=85%。宝箱格子可设为 0.85")]
    [SerializeField] [Range(0.3f, 1f)] private float iconSizeRatio = 1f;

    [Header("Preview")]
    [SerializeField] private Image previewIconImage;
    [SerializeField] private float previewAlpha = 0.5f;

    public ItemDataSO currentItem;

    private float _lastLeftClickTime = -10f;
    private bool _isPointerInside;

    private void Update()
    {
        if (_isPointerInside)
            RefreshPreviewIcon();
    }

    public void OnDrop(PointerEventData eventData)
    {
        HidePreviewIcon();

        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj == null || inventoryData == null || !IsValidSlot(inventoryData, slotIndex))
            return;

        ItemUI draggedItem = draggedObj.GetComponent<ItemUI>();
        if (draggedItem == null || draggedItem.itemData == null)
            return;

        InventorySlotUI sourceSlot = ResolveSourceInventorySlot(draggedObj);
        if (sourceSlot != null && sourceSlot.inventoryData != null && IsValidSlot(sourceSlot.inventoryData, sourceSlot.slotIndex))
        {
            bool sameSlot = sourceSlot.inventoryData == inventoryData && sourceSlot.slotIndex == slotIndex;
            if (sameSlot)
                return;

            ItemDataSO sourceItem = sourceSlot.inventoryData.Slots[sourceSlot.slotIndex].itemData;
            ItemDataSO targetItem = inventoryData.Slots[slotIndex].itemData;
            if (sourceItem == null)
                return;

            sourceSlot.inventoryData.SetItemAt(sourceSlot.slotIndex, targetItem);
            inventoryData.SetItemAt(slotIndex, sourceItem);
            Destroy(draggedObj);
            return;
        }

        // Drag from non-inventory source (e.g. assembly): only place into empty inventory slot.
        if (!inventoryData.Slots[slotIndex].IsEmpty)
            return;

        inventoryData.SetItemAt(slotIndex, draggedItem.itemData);
        Destroy(draggedObj);
    }

    public void UpdateSlotDisplay(InventorySlot slot, int index)
    {
        slotIndex = index;

        ItemUI currentItemUI = GetComponentInChildren<ItemUI>();

        if (slot.IsEmpty)
        {
            if (currentItemUI != null)
                Destroy(currentItemUI.gameObject);

            currentItem = null;
            return;
        }

        if (currentItemUI == null)
        {
            GameObject obj = Instantiate(itemIconPrefab, transform);
            currentItemUI = obj.GetComponent<ItemUI>();
            ApplyIconSize(currentItemUI.GetComponent<RectTransform>());
        }

        currentItemUI.SetItem(slot.itemData);
        currentItem = slot.itemData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;

        if (currentItem != null)
            TooltipManager.Instance.ShowTooltip(currentItem.icon, currentItem.itemName, currentItem.useInfo);

        RefreshPreviewIcon();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        TooltipManager.Instance.Hide();
        HidePreviewIcon();
    }

    public void OnDisable()
    {
        _isPointerInside = false;
        HidePreviewIcon();

        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (eventData.button != PointerEventData.InputButton.Left || currentItem == null)
            return;

        float now = Time.unscaledTime;
        bool isDoubleClick = now - _lastLeftClickTime <= DoubleClickThreshold;
        _lastLeftClickTime = now;

        if (isDoubleClick)
        {
            TryMoveToFirstEmptySlot();
            return;
        }

        if (currentItem.itemType == ItemType.InteractiveProp && SceneManager.GetActiveScene().name == "HomeScene")
        {
            var go = GameObject.Find(currentItem.timelineObjectName);
            var director = go != null ? go.GetComponent<PlayableDirector>() : null;
            if (director != null && HomeSceneEntryTimelineTrigger.TryPlayPlatformTimelineFromClick(currentItem, director))
                return;
            UseSpecialItem();
        }
    }

    private void UseSpecialItem()
    {
        ItemDataSO item = currentItem;
        Debug.Log($"Used special item: {item.itemName}");

        if (!string.IsNullOrEmpty(item.timelineObjectName))
        {
            GameObject go = GameObject.Find(item.timelineObjectName);
            if (go != null)
            {
                PlayableDirector director = go.GetComponent<PlayableDirector>();
                if (director != null)
                    TimelineManager.Instance.Play(director);
                else
                    Debug.LogWarning($"[InventorySlotUI] GameObject '{item.timelineObjectName}' has no PlayableDirector");
            }
            else
            {
                Debug.LogWarning($"[InventorySlotUI] Cannot find GameObject '{item.timelineObjectName}'");
            }
        }

        inventoryData.RemoveItem(item);
        currentItem = null;
        TooltipManager.Instance?.Hide();
    }

    private void TryMoveToFirstEmptySlot()
    {
        if (inventoryData == null || !IsValidSlot(inventoryData, slotIndex))
            return;

        ItemDataSO item = inventoryData.Slots[slotIndex].itemData;
        if (item == null)
            return;

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            if (i == slotIndex)
                continue;

            if (!inventoryData.Slots[i].IsEmpty)
                continue;

            inventoryData.SetItemAt(i, item);
            inventoryData.SetItemAt(slotIndex, null);
            return;
        }
    }

    private void RefreshPreviewIcon()
    {
        if (!_isPointerInside || previewIconImage == null)
        {
            HidePreviewIcon();
            return;
        }

        if (!InventoryDragContext.IsDragging || InventoryDragContext.Icon == null)
        {
            HidePreviewIcon();
            return;
        }

        if (InventoryDragContext.SourceSlot == this)
        {
            HidePreviewIcon();
            return;
        }

        previewIconImage.sprite = InventoryDragContext.Icon;
        Color c = Color.white;
        c.a = Mathf.Clamp01(previewAlpha);
        previewIconImage.color = c;
        previewIconImage.raycastTarget = false;
        previewIconImage.gameObject.SetActive(true);

        SetSlotIconVisible(false);
    }

    private void HidePreviewIcon()
    {
        if (previewIconImage != null)
            previewIconImage.gameObject.SetActive(false);

        SetSlotIconVisible(true);
    }

    private void SetSlotIconVisible(bool visible)
    {
        ItemUI itemUI = GetComponentInChildren<ItemUI>();
        if (itemUI == null || itemUI.iconImage == null)
            return;

        itemUI.iconImage.enabled = visible;
    }

    private void ApplyIconSize(RectTransform iconRect)
    {
        if (iconRect == null)
            return;

        RectTransform slotRect = transform as RectTransform;
        if (slotRect == null)
            return;

        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;

        float slotSize = Mathf.Min(slotRect.rect.width, slotRect.rect.height);
        float size = slotSize * iconSizeRatio;
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.localScale = Vector3.one;
    }

    private static bool IsValidSlot(IStorageData data, int index)
    {
        return data != null && index >= 0 && index < data.Slots.Count;
    }

    private static InventorySlotUI ResolveSourceInventorySlot(GameObject draggedObj)
    {
        UIDragHandler dragHandler = draggedObj.GetComponent<UIDragHandler>();
        if (dragHandler != null && dragHandler.originalParent != null)
        {
            InventorySlotUI byOriginalParent = dragHandler.originalParent.GetComponent<InventorySlotUI>();
            if (byOriginalParent != null)
                return byOriginalParent;
        }

        return draggedObj.GetComponentInParent<InventorySlotUI>();
    }
}
