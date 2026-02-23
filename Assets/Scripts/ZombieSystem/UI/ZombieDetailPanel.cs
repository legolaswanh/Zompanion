using TMPro;
using UnityEngine;

public class ZombieDetailPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text buffText;
    [SerializeField] private TMP_Text storyText;

    public void Bind(ZombieInstanceData zombie, ZombieDefinitionSO definition, bool storyUnlocked)
    {
        if (zombie == null)
        {
            Clear();
            return;
        }

        if (titleText != null)
            titleText.text = zombie.displayName;

        if (idText != null)
            idText.text = $"ID: {zombie.instanceId}";

        if (typeText != null)
            typeText.text = definition != null ? $"Type: {definition.Type} / {definition.Category}" : "Type: Unknown";

        if (stateText != null)
            stateText.text = $"State: {zombie.state}";

        if (buffText != null)
            buffText.text = $"Buff: {zombie.appliedBuffType} ({zombie.appliedBuffValue:0.##})";

        if (storyText != null)
            storyText.text = storyUnlocked ? "Story: Unlocked" : "Story: Locked";
    }

    public void Clear()
    {
        if (titleText != null) titleText.text = "No Selection";
        if (idText != null) idText.text = "ID: -";
        if (typeText != null) typeText.text = "Type: -";
        if (stateText != null) stateText.text = "State: -";
        if (buffText != null) buffText.text = "Buff: -";
        if (storyText != null) storyText.text = "Story: -";
    }
}
