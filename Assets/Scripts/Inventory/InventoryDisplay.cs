using System.Collections.Generic;
using Code.Scripts;
using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool usePanelCoordinator;

    private readonly List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();

    public bool IsVisible
    {
        get
        {
            GameObject panel = GetPanelRoot();
            return panel != null && panel.activeSelf;
        }
    }

    private void Awake()
    {
        ResolveInventoryReference();

        GameObject panel = GetPanelRoot();
        if (usePanelCoordinator && panel != null)
            UIPanelCoordinator.Register(panel);
    }

    private void Start()
    {
        ResolveInventoryReference();
        if (inventoryData == null)
        {
            Debug.LogWarning("[InventoryDisplay] Inventory reference is missing.");
            return;
        }

        inventoryData.OnInventoryUpdated += UpdateDisplay;
        InitializeInventoryUI();
    }

    private void OnDestroy()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryUpdated -= UpdateDisplay;
    }

    private void InitializeInventoryUI()
    {
        if (inventoryData == null || slotsContainer == null)
            return;

        inventoryData.Initialize();
        RebuildSlots(inventoryData.slots.Count);
        UpdateDisplay();
    }

    private void RebuildSlots(int count)
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("[InventoryDisplay] Slot prefab or slots container is missing.");
            return;
        }

        for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        slotUIList.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = obj.GetComponent<InventorySlotUI>();
            if (slotUI == null)
                continue;

            slotUI.inventoryData = inventoryData;
            slotUI.slotIndex = i;
            slotUIList.Add(slotUI);
        }
    }

    public void UpdateDisplay()
    {
        if (inventoryData == null)
            return;

        if (slotUIList.Count != inventoryData.slots.Count)
            RebuildSlots(inventoryData.slots.Count);

        int count = Mathf.Min(slotUIList.Count, inventoryData.slots.Count);
        for (int i = 0; i < count; i++)
            slotUIList[i].UpdateSlotDisplay(inventoryData.slots[i], i);
    }

    public void ShowInventory()
    {
        SetInventoryVisible(true);
    }

    public void HideInventory()
    {
        SetInventoryVisible(false);
    }

    public void ToggleInventory()
    {
        SetInventoryVisible(!IsVisible);
    }

    /// <summary>根据物品查找对应的 slot UI，用于外部高亮/闪烁等。</summary>
    public InventorySlotUI TryGetSlotUIForItem(ItemDataSO item)
    {
        if (inventoryData == null || item == null || slotUIList == null) return null;
        for (int i = 0; i < inventoryData.Slots.Count && i < slotUIList.Count; i++)
        {
            var s = inventoryData.Slots[i];
            if (s != null && !s.IsEmpty && s.itemData == item)
                return slotUIList[i];
        }
        return null;
    }

    public void SetInventoryVisible(bool visible)
    {
        GameObject panel = GetPanelRoot();
        if (panel == null)
            return;

        if (usePanelCoordinator)
        {
            if (visible)
                UIPanelCoordinator.ShowExclusive(panel);
            else
                UIPanelCoordinator.Hide(panel);

            return;
        }

        panel.SetActive(visible);
    }

    private GameObject GetPanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        return panelRoot;
    }

    private void ResolveInventoryReference()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInventory == null)
            return;

        inventoryData = GameManager.Instance.PlayerInventory;
    }
}
