using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ZombieEntryView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    [SerializeField] private Color disabledBackgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private CanvasGroup canvasGroup;

    private string _definitionId;
    private Action<string> _onSelectDefinition;
    private Action<string> _onDoubleClickDefinition;
    private bool _isInteractable;
    private bool _isDragging;

    public string DefinitionId => _definitionId;

    public void SetupDefinition(
        ZombieDefinitionSO definition,
        bool unlocked,
        bool isFollowing,
        bool selected,
        Action<string> onSelect,
        bool interactable = true,
        string stateOverride = null,
        Action<string> onDoubleClick = null)
    {
        _definitionId = definition != null ? definition.DefinitionId : string.Empty;
        _onSelectDefinition = onSelect;
        _onDoubleClickDefinition = onDoubleClick;
        _isInteractable = definition != null && _onSelectDefinition != null && interactable;
        _isDragging = false;
        ApplySelectionVisual(selected, _isInteractable);

        if (nameText != null)
            nameText.text = unlocked && definition != null ? definition.DisplayName : "???";

        if (typeText != null)
            typeText.text = unlocked && definition != null ? definition.Category.ToString() : "Locked";

        if (stateText != null)
        {
            if (!string.IsNullOrWhiteSpace(stateOverride))
                stateText.text = stateOverride;
            else if (!unlocked)
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
            selectButton.interactable = _isInteractable;
            selectButton.onClick.AddListener(() => _onSelectDefinition?.Invoke(_definitionId));
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractable || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (eventData.clickCount >= 2)
            _onDoubleClickDefinition?.Invoke(_definitionId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isInteractable || eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        _isDragging = true;
        ZombieDragContext.Begin(_definitionId, iconImage != null ? iconImage.sprite : null);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Drag source only; no visual ghost is required for current scope.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        _isDragging = false;
        ZombieDragContext.Clear();
    }

    private void OnDisable()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (_isDragging)
        {
            _isDragging = false;
            ZombieDragContext.Clear();
        }
    }

    private void ApplySelectionVisual(bool selected, bool interactable)
    {
        if (backgroundImage == null)
            return;

        if (!interactable)
        {
            backgroundImage.color = disabledBackgroundColor;
            return;
        }

        backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
    }
}
