using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DiggingTrigger : MonoBehaviour, ISaveable
{
    [Serializable]
    public class DiggingTriggerState
    {
        public bool isDug;
        public List<string> assignedItemNames;
        public bool isActive;
        public bool colliderEnabled;
    }

    [Header("Runtime State")]
    [SerializeField] public bool isCustomizedPoint = false;
    [SerializeField] private List<ItemDataSO> assignedItems = new List<ItemDataSO>();

    [Header("Config")]
    [SerializeField] private Sprite dugSprite;

    [Header("UI")]
    public Canvas buttonCanvas;
    public GameObject itemDisplayUI;

    private bool isDug;
    private bool isInteractionLocked;
    private bool waitingForPopupClose;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private ItemDataSO pendingItem;
    private InventorySO pendingInventory;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Player"))
            return;

        if (buttonCanvas != null)
            buttonCanvas.gameObject.SetActive(true);

        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.SetCurrentTrigger(gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Player"))
            return;

        if (buttonCanvas != null)
            buttonCanvas.gameObject.SetActive(false);

        if (PlayerInteraction.Instance != null)
            PlayerInteraction.Instance.ClearCurrentTrigger(gameObject);
    }

    public void SetContent(List<ItemDataSO> items)
    {
        assignedItems = items != null ? new List<ItemDataSO>(items) : new List<ItemDataSO>();
        isDug = false;
        isInteractionLocked = false;
        waitingForPopupClose = false;
        pendingItem = null;
        pendingInventory = null;
    }

    public void AddContent(ItemDataSO item)
    {
        if (assignedItems == null)
            assignedItems = new List<ItemDataSO>();

        assignedItems.Add(item);
    }

    public void Interact(InventorySO playerInventory)
    {
        if (isDug || isInteractionLocked)
            return;

        if (assignedItems == null || assignedItems.Count == 0)
        {
            FinishDigging();
            return;
        }

        ItemDataSO dugItem = assignedItems[0];
        if (dugItem == null)
        {
            assignedItems.RemoveAt(0);
            CompleteInteraction();
            return;
        }

        pendingItem = dugItem;
        pendingInventory = playerInventory;
        isInteractionLocked = true;
        waitingForPopupClose = true;

        ShowItemPopup(dugItem);
    }

    private void ShowItemPopup(ItemDataSO dugItem)
    {
        if (itemDisplayUI != null)
        {
            GameObject uiInstance = Instantiate(itemDisplayUI);
            ItemDisplayUI displayUI = uiInstance != null
                ? uiInstance.GetComponent<ItemDisplayUI>() ?? uiInstance.GetComponentInChildren<ItemDisplayUI>(true)
                : null;
            if (displayUI != null)
            {
                displayUI.ShowItem(
                    dugItem.icon,
                    dugItem.itemName,
                    dugItem.description,
                    HandlePopupClosed);
                return;
            }

            Debug.LogWarning("[DiggingTrigger] itemDisplayUI prefab is missing ItemDisplayUI component.");
        }
        else
        {
            Debug.LogWarning("[DiggingTrigger] itemDisplayUI is not assigned on this digging trigger.");
        }

        // Fallback: if popup prefab/component missing, proceed immediately.
        HandlePopupClosed();
    }

    private void HandlePopupClosed()
    {
        if (!waitingForPopupClose)
            return;

        waitingForPopupClose = false;

        ItemDataSO dugItem = pendingItem;
        InventorySO inventory = pendingInventory;

        pendingItem = null;
        pendingInventory = null;

        bool success = inventory != null && dugItem != null && inventory.AddItem(dugItem);
        if (success)
        {
            if (assignedItems != null && assignedItems.Count > 0)
            {
                if (assignedItems[0] == dugItem)
                    assignedItems.RemoveAt(0);
                else
                    assignedItems.Remove(dugItem);
            }

            CustomizedPoint(dugItem);
        }
        else
        {
            Debug.Log("[DiggingTrigger] Backpack full or inventory missing. Item stays in digging point.");
        }

        CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        isInteractionLocked = false;

        if (assignedItems == null || assignedItems.Count == 0)
            FinishDigging();
    }

    private void FinishDigging()
    {
        isDug = true;
        isInteractionLocked = false;
        waitingForPopupClose = false;

        if (dugSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = dugSprite;

        if (col != null)
            col.enabled = false;

        gameObject.SetActive(false);
    }

    public string CaptureState()
    {
        var names = new List<string>();
        if (assignedItems != null)
        {
            foreach (ItemDataSO item in assignedItems)
                names.Add(item != null ? item.itemName : null);
        }

        var state = new DiggingTriggerState
        {
            isDug = isDug,
            assignedItemNames = names,
            isActive = gameObject.activeSelf,
            colliderEnabled = col != null && col.enabled
        };
        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string stateJson)
    {
        DiggingTriggerState state = JsonUtility.FromJson<DiggingTriggerState>(stateJson);
        if (state == null)
            return;

        isDug = state.isDug;
        isInteractionLocked = false;
        waitingForPopupClose = false;
        pendingItem = null;
        pendingInventory = null;

        assignedItems = new List<ItemDataSO>();
        if (state.assignedItemNames != null)
        {
            foreach (string name in state.assignedItemNames)
            {
                ItemDataSO item = ItemLookup.Get(name);
                if (item != null)
                    assignedItems.Add(item);
            }
        }

        if (isDug && dugSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = dugSprite;

        if (col != null)
            col.enabled = state.colliderEnabled;

        gameObject.SetActive(state.isActive);
    }

    public void CustomizedPoint(ItemDataSO dugItem)
    {
        if (!isCustomizedPoint || dugItem == null)
            return;

        switch (dugItem.itemName)
        {
            case "A Mysterious Book":
                DialogueLua.SetVariable("hasBook", true);
                break;
            case "Dead Man's Arms":
                DialogueLua.SetVariable("hasFirstArm", true);
                break;
        }

        DialogueSystemTrigger[] allTriggers = GetComponents<DialogueSystemTrigger>();
        foreach (DialogueSystemTrigger trigger in allTriggers)
        {
            if (trigger != null && trigger.enabled)
                trigger.OnUse();
        }
    }
}
