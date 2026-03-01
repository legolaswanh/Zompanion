using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zompanion.ZombieSystem;

public class ZombieEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button followButton;
    [SerializeField] private Button workButton;
    [SerializeField] private TMP_Text followButtonText;
    [SerializeField] private TMP_Text workButtonText;

    private int _instanceId;
    private string _definitionId;
    private Action<int> _onSelect;
    private Action<int> _onToggleFollow;
    private Action<int> _onToggleWork;
    private Action<string> _onSelectDefinition;
    private Action<string> _onToggleDefinitionFollow;

    public void Setup(
        ZombieInstanceData zombie,
        ZombieDefinitionSO definition,
        Action<int> onSelect,
        Action<int> onToggleFollow,
        Action<int> onToggleWork)
    {
        _instanceId = zombie.instanceId;
        _onSelect = onSelect;
        _onToggleFollow = onToggleFollow;
        _onToggleWork = onToggleWork;

        if (nameText != null)
            nameText.text = zombie.displayName;

        if (typeText != null)
            typeText.text = definition != null ? definition.Type.ToString() : "Unknown";

        if (stateText != null)
            stateText.text = zombie.state.ToString();

        if (followButtonText != null)
            followButtonText.text = zombie.state == ZombieState.Following ? "Unfollow" : "Follow";

        if (workButtonText != null)
            workButtonText.text = zombie.state == ZombieState.Working ? "Stop Work" : "Work";

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelect?.Invoke(_instanceId));
        }

        if (followButton != null)
        {
            followButton.onClick.RemoveAllListeners();
            followButton.onClick.AddListener(() => _onToggleFollow?.Invoke(_instanceId));
        }

        if (workButton != null)
        {
            workButton.onClick.RemoveAllListeners();
            workButton.onClick.AddListener(() => _onToggleWork?.Invoke(_instanceId));
        }
    }

    public void SetupDefinition(
        ZombieDefinitionSO definition,
        bool unlocked,
        bool isFollowing,
        bool selected,
        Sprite lockedIcon,
        Action<string> onSelect,
        Action<string> onToggleFollow)
    {
        _definitionId = definition != null ? definition.DefinitionId : string.Empty;
        _onSelectDefinition = onSelect;
        _onToggleDefinitionFollow = onToggleFollow;

        if (nameText != null)
            nameText.text = unlocked && definition != null ? definition.DisplayName : "???";

        if (typeText != null)
            typeText.text = unlocked && definition != null ? definition.Type.ToString() : "Locked";

        if (stateText != null)
        {
            if (!unlocked)
                stateText.text = "Locked";
            else
                stateText.text = isFollowing ? "Following" : "Owned";
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
            selectButton.interactable = definition != null;
            selectButton.onClick.AddListener(() => _onSelectDefinition?.Invoke(_definitionId));
        }

        if (followButton != null)
        {
            followButton.onClick.RemoveAllListeners();
            followButton.interactable = unlocked && definition != null && _onToggleDefinitionFollow != null;
            followButton.onClick.AddListener(() => _onToggleDefinitionFollow?.Invoke(_definitionId));
        }

        if (followButtonText != null)
        {
            if (!unlocked)
                followButtonText.text = "Locked";
            else if (_onToggleDefinitionFollow != null)
                followButtonText.text = isFollowing ? "Unfollow" : "Follow";
            else
                followButtonText.text = string.Empty;
        }

        if (workButton != null)
            workButton.gameObject.SetActive(false);

        if (workButtonText != null)
            workButtonText.text = selected ? "Selected" : string.Empty;
    }
}
