using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombieEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button selectButton;

    private string _definitionId;
    private Action<string> _onSelectDefinition;

    public void SetupDefinition(
        ZombieDefinitionSO definition,
        bool unlocked,
        bool isFollowing,
        bool selected,
        Sprite lockedIcon,
        Action<string> onSelect)
    {
        _definitionId = definition != null ? definition.DefinitionId : string.Empty;
        _onSelectDefinition = onSelect;

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
            Sprite icon = unlocked && definition != null ? definition.CodexIcon : lockedIcon;
            if (icon != null)
                iconImage.sprite = icon;
            iconImage.color = unlocked ? Color.white : Color.black;
        }

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.interactable = definition != null && _onSelectDefinition != null;
            selectButton.onClick.AddListener(() => _onSelectDefinition?.Invoke(_definitionId));
        }
    }
}
