using System.Collections.Generic;
using Code.Scripts;
using UnityEngine;

/// <summary>
/// 通用储物 UI 展示，可绑定 IStorageData（如宝箱）。
/// 储物数据由外部在运行时设置，不通过 SerializeField。
/// </summary>
public class StorageDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("使用预生成格子时留空，使用动态生成时拖入 Slot 预制体")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool usePanelCoordinator;

    [Header("预生成格子")]
    [Tooltip("勾选则使用 slotsContainer 下已有的子物体作为格子，不销毁、不实例化；不勾选则动态实例化 slotPrefab")]
    [SerializeField] private bool useExistingSlots;

    private IStorageData _storage;
    private readonly List<InventorySlotUI> _slotUIList = new List<InventorySlotUI>();

    public IStorageData Storage
    {
        get => _storage;
        set
        {
            if (_storage == value)
            {
                UpdateDisplay();
                return;
            }

            if (_storage != null)
                _storage.OnInventoryUpdated -= UpdateDisplay;

            _storage = value;

            if (_storage != null)
            {
                _storage.OnInventoryUpdated += UpdateDisplay;
                InitializeSlots();
            }
        }
    }

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
        GameObject panel = GetPanelRoot();
        if (usePanelCoordinator && panel != null)
            UIPanelCoordinator.Register(panel);
    }

    private void OnDestroy()
    {
        if (_storage != null)
            _storage.OnInventoryUpdated -= UpdateDisplay;
    }

    private void InitializeSlots()
    {
        if (_storage == null || slotsContainer == null)
            return;

        if (useExistingSlots)
        {
            BindExistingSlots();
        }
        else
        {
            if (slotPrefab == null)
                return;
            RebuildSlots(_storage.Slots.Count);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 使用 slotsContainer 下已有的子物体作为格子，绑定 Storage 与 slotIndex。
    /// 每个子物体需挂载 InventorySlotUI，itemIconPrefab 在有物品时会动态生成到格子下（与背包逻辑一致）。
    /// 注意：ChestInteractTrigger 的 Capacity 需设为与预生成格子数量一致（如 10）。
    /// </summary>
    private void BindExistingSlots()
    {
        _slotUIList.Clear();

        int childCount = slotsContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = slotsContainer.GetChild(i);
            InventorySlotUI slotUI = child.GetComponent<InventorySlotUI>();
            if (slotUI == null)
            {
                Debug.LogWarning("[StorageDisplay] " + child.name + " 缺少 InventorySlotUI，已跳过。请在格子预制体上添加 InventorySlotUI 并配置 itemIconPrefab（Item_Prefab）。");
                continue;
            }

            slotUI.inventoryData = _storage;
            slotUI.slotIndex = i;
            _slotUIList.Add(slotUI);
        }
    }

    private void RebuildSlots(int count)
    {
        if (slotsContainer == null || slotPrefab == null)
            return;

        for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        _slotUIList.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = obj.GetComponent<InventorySlotUI>();
            if (slotUI == null)
                continue;

            slotUI.inventoryData = _storage;
            slotUI.slotIndex = i;
            _slotUIList.Add(slotUI);
        }
    }

    public void UpdateDisplay()
    {
        if (_storage == null)
            return;

        if (!useExistingSlots && _slotUIList.Count != _storage.Slots.Count)
            RebuildSlots(_storage.Slots.Count);

        int count = Mathf.Min(_slotUIList.Count, _storage.Slots.Count);
        for (int i = 0; i < count; i++)
            _slotUIList[i].UpdateSlotDisplay(_storage.Slots[i], i);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
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
        }
        else
        {
            panel.SetActive(visible);
        }
    }

    private GameObject GetPanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
        return panelRoot;
    }
}
