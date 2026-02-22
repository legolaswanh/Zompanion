using System.Collections.Generic;
using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;

    private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();

    private void Start()
    {
        if (inventoryData.clearOnStart)
        {
            inventoryData.ClearAll();
        }
        
        // 1. 订阅事件：当数据变化时，自动刷新 UI
        inventoryData.OnInventoryUpdated += UpdateDisplay;

        // 2. 初始化显示
        InitializeInventoryUI();
    }

    private void OnDestroy()
    {
        // 养成好习惯：对象销毁时取消订阅，防止内存泄漏
        inventoryData.OnInventoryUpdated -= UpdateDisplay;
    }

    // 初始化：根据背包容量生成对应数量的格子
    private void InitializeInventoryUI()
    {
        // 清理现有的测试子物体（如果有）
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIList.Clear();

        // 确保数据已初始化
        inventoryData.Initialize();

        // 根据 MaxCapacity (10个) 生成格子
        for (int i = 0; i < inventoryData.MaxCapacity; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = obj.GetComponent<InventorySlotUI>();
            
            if (slotUI != null)
            {
                slotUIList.Add(slotUI);
            }
        }

        // 第一次强制刷新
        UpdateDisplay();
    }

    // 刷新：遍历所有 UI 格子，让它们去读最新的数据
    public void UpdateDisplay()
    {
        for (int i = 0; i < slotUIList.Count; i++)
        {
            // 保护机制：防止 UI 格子数量和数据不匹配
            if (i < inventoryData.slots.Count)
            {
                slotUIList[i].UpdateSlotDisplay(inventoryData.slots[i], i);
            }
        }
    }
}