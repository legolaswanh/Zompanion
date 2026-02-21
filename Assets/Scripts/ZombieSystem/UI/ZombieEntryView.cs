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
    [SerializeField] private Button selectButton;
    [SerializeField] private Button followButton;
    [SerializeField] private Button workButton;
    [SerializeField] private TMP_Text followButtonText;
    [SerializeField] private TMP_Text workButtonText;

    private int _instanceId;
    private Action<int> _onSelect;
    private Action<int> _onToggleFollow;
    private Action<int> _onToggleWork;

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
}

