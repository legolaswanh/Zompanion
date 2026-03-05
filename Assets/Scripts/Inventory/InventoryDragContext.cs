using UnityEngine;

public static class InventoryDragContext
{
    public static bool IsDragging { get; private set; }
    public static InventorySlotUI SourceSlot { get; private set; }
    public static Sprite Icon { get; private set; }

    public static void Begin(InventorySlotUI sourceSlot, Sprite icon)
    {
        IsDragging = true;
        SourceSlot = sourceSlot;
        Icon = icon;
    }

    public static void Clear()
    {
        IsDragging = false;
        SourceSlot = null;
        Icon = null;
    }
}
