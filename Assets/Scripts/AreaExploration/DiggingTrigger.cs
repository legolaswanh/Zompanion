using System.Collections.Generic;
using UnityEngine;

public class DiggingTrigger : MonoBehaviour
{
    [Header("Runtime State (Auto Assigned)")]
    [SerializeField] private List<ItemDataSO> assignedItems = new List<ItemDataSO>();

    [Header("Config")]
    [SerializeField] private Sprite dugSprite;

    [Header("Optional UI")]
    public Canvas buttonCanvas;

    private bool isDug;
    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Player")) return;

        if (buttonCanvas != null)
            buttonCanvas.gameObject.SetActive(true);

        PlayerInteraction.Instance?.SetCurrentTrigger(this);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Player")) return;

        if (buttonCanvas != null)
            buttonCanvas.gameObject.SetActive(false);

        PlayerInteraction.Instance?.ClearCurrentTrigger(this);
    }

    public void SetContent(List<ItemDataSO> items)
    {
        assignedItems = items != null ? new List<ItemDataSO>(items) : new List<ItemDataSO>();
        isDug = false;
    }

    public void AddContent(ItemDataSO item)
    {
        if (item == null) return;

        if (assignedItems == null)
            assignedItems = new List<ItemDataSO>();

        assignedItems.Add(item);
    }

    public void Interact(InventorySO playerInventory)
    {
        if (isDug || playerInventory == null) return;

        if (assignedItems == null || assignedItems.Count == 0)
        {
            Debug.Log("Nothing found at this spot.");
            FinishDigging();
            return;
        }

        ItemDataSO itemToGive = assignedItems[0];
        if (itemToGive == null)
        {
            assignedItems.RemoveAt(0);
            if (assignedItems.Count == 0)
                FinishDigging();
            return;
        }

        bool success = playerInventory.AddItem(itemToGive, 1);
        if (!success)
        {
            Debug.Log("Inventory is full.");
            return;
        }

        Debug.Log($"Found {itemToGive.itemName} (remaining: {assignedItems.Count - 1})");
        assignedItems.RemoveAt(0);

        if (assignedItems.Count == 0)
            FinishDigging();
    }

    private void FinishDigging()
    {
        isDug = true;

        if (dugSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = dugSprite;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (buttonCanvas != null)
            buttonCanvas.gameObject.SetActive(false);
    }
}
