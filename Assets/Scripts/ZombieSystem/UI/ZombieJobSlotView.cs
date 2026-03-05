using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ZombieJobSlotView : MonoBehaviour, IPointerClickHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [FormerlySerializedAs("portraitImage")]
    [SerializeField] private Image slotIconImage;
    [SerializeField] private Image previewIconImage;
    [SerializeField] private TMP_Text slotLabelText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Styles")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private float previewAlpha = 0.5f;

    private int _slotIndex;
    private string _definitionId;
    private Action<int> _onSelect;
    private Action<int> _onClear;
    private Action<int, string> _onDropDefinition;
    private Action<int, int> _onSwapSlots;
    private bool _isDragging;
    private bool _suppressClickAfterDrag;
    private bool _isPointerInside;

    public void Setup(
        int slotIndex,
        ZombieDefinitionSO definition,
        bool selected,
        Action<int> onSelect,
        Action<int> onClear,
        Action<int, string> onDropDefinition,
        Action<int, int> onSwapSlots)
    {
        _slotIndex = slotIndex;
        _definitionId = definition != null ? definition.DefinitionId : string.Empty;
        _onSelect = onSelect;
        _onClear = onClear;
        _onDropDefinition = onDropDefinition;
        _onSwapSlots = onSwapSlots;
        _isDragging = false;
        _suppressClickAfterDrag = false;
        _isPointerInside = false;

        if (slotLabelText != null)
            slotLabelText.text = $"SLOT {slotIndex + 1}";

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;

        if (slotIconImage != null)
        {
            Sprite icon = definition != null ? definition.CodexIcon : null;
            slotIconImage.sprite = icon;
            slotIconImage.color = icon != null ? Color.white : Color.clear;
            slotIconImage.enabled = true;
        }

        HidePreviewIcon();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (_suppressClickAfterDrag && eventData.button == PointerEventData.InputButton.Left)
        {
            _suppressClickAfterDrag = false;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _onSelect?.Invoke(_slotIndex);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
            _onClear?.Invoke(_slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
            return;

        HidePreviewIcon();

        ZombieJobSlotView sourceSlot = eventData.pointerDrag.GetComponentInParent<ZombieJobSlotView>();
        if (sourceSlot != null && _onSwapSlots != null)
        {
            _onSwapSlots.Invoke(sourceSlot._slotIndex, _slotIndex);
            return;
        }

        if (_onDropDefinition == null)
            return;

        ZombieEntryView entry = eventData.pointerDrag.GetComponentInParent<ZombieEntryView>();
        if (entry == null || string.IsNullOrWhiteSpace(entry.DefinitionId))
            return;

        _onDropDefinition.Invoke(_slotIndex, entry.DefinitionId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (string.IsNullOrWhiteSpace(_definitionId) || _onSwapSlots == null)
            return;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _isDragging = true;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        ZombieDragContext.Begin(_definitionId, slotIconImage != null ? slotIconImage.sprite : null);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Drag source only; destination handles drop.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        _suppressClickAfterDrag = true;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        ZombieDragContext.Clear();
        HidePreviewIcon();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        RefreshPreviewIcon();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        HidePreviewIcon();
    }

    private void RefreshPreviewIcon()
    {
        if (_isDragging || !_isPointerInside || !ZombieDragContext.IsDragging)
        {
            HidePreviewIcon();
            return;
        }

        if (previewIconImage == null)
            return;

        Sprite icon = ZombieDragContext.Icon;
        if (icon == null)
        {
            HidePreviewIcon();
            return;
        }

        previewIconImage.sprite = icon;
        Color color = Color.white;
        color.a = Mathf.Clamp01(previewAlpha);
        previewIconImage.color = color;
        previewIconImage.gameObject.SetActive(true);
        previewIconImage.raycastTarget = false;
        if (slotIconImage != null)
            slotIconImage.enabled = false;
    }

    private void HidePreviewIcon()
    {
        if (previewIconImage != null)
            previewIconImage.gameObject.SetActive(false);
        if (slotIconImage != null)
            slotIconImage.enabled = true;
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

        _isPointerInside = false;
        HidePreviewIcon();
    }
}
