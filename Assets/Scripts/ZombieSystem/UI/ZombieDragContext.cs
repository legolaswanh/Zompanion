using UnityEngine;

public static class ZombieDragContext
{
    public static bool IsDragging { get; private set; }
    public static string DefinitionId { get; private set; }
    public static Sprite Icon { get; private set; }

    public static void Begin(string definitionId, Sprite icon)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            Clear();
            return;
        }

        IsDragging = true;
        DefinitionId = definitionId;
        Icon = icon;
    }

    public static void Clear()
    {
        IsDragging = false;
        DefinitionId = null;
        Icon = null;
    }
}
