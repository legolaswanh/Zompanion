using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombieEntryView : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private string _definitionId;
    private Action<string> _onSelectDefinition;

    public void SetupDefinition(
        ZombieDefinitionSO definition,
        bool unlocked,
        bool isFollowing,
        bool selected,
        Action<string> onSelect)
    {
        _definitionId = definition != null ? definition.DefinitionId : string.Empty;
        _onSelectDefinition = onSelect;
        ApplySelectionVisual(selected);

        if (nameText != null)
            nameText.text = unlocked && definition != null ? definition.DisplayName : "???";

        if (typeText != null)
            typeText.text = unlocked && definition != null ? definition.Type.ToString() : "Locked";

        if (stateText != null)
        {
            if (!unlocked)
                stateText.text = "Locked";
            else if (isFollowing)
                stateText.text = selected ? "Following - Selected" : "Following";
            else
                stateText.text = selected ? "Owned - Selected" : "Owned";
        }

        if (iconImage != null)
        {
            Sprite icon = definition != null ? definition.CodexIcon : null;
            iconImage.sprite = icon;
            iconImage.color = icon != null ? (unlocked ? Color.white : Color.black) : Color.clear;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.interactable = definition != null && _onSelectDefinition != null;
            selectButton.onClick.AddListener(() => _onSelectDefinition?.Invoke(_definitionId));
        }
    }

    private void ApplySelectionVisual(bool selected)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
    }
}
