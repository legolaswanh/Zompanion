using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ZombieJobSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [FormerlySerializedAs("portraitImage")]
    [SerializeField] private Image slotIconImage;
    [SerializeField] private TMP_Text slotLabelText;

    [Header("Styles")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private int _slotIndex;
    private Action<int> _onSelect;
    private Action<int> _onClear;

    public void Setup(
        int slotIndex,
        ZombieDefinitionSO definition,
        bool selected,
        Action<int> onSelect,
        Action<int> onClear)
    {
        _slotIndex = slotIndex;
        _onSelect = onSelect;
        _onClear = onClear;

        if (slotLabelText != null)
            slotLabelText.text = $"SLOT {slotIndex + 1}";

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;

        if (slotIconImage != null)
        {
            Sprite icon = definition != null ? definition.CodexIcon : null;
            slotIconImage.sprite = icon;
            slotIconImage.color = icon != null ? Color.white : Color.clear;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _onSelect?.Invoke(_slotIndex);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
            _onClear?.Invoke(_slotIndex);
    }
}
