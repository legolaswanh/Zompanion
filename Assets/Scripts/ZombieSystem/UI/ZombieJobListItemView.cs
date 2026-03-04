using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombieJobListItemView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform lightRoot;
    [SerializeField] private Image lightPrefab;

    [Header("Styles")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Sprite lightOffSprite;
    [SerializeField] private Sprite lightOnSprite;

    private readonly List<Image> _lightInstances = new List<Image>();
    private string _jobId;
    private Action<string> _onSelect;

    public void Setup(
        string jobId,
        string displayName,
        Sprite icon,
        int slotCount,
        int assignedCount,
        bool selected,
        Action<string> onSelect)
    {
        _jobId = jobId;
        _onSelect = onSelect;

        if (nameText != null)
            nameText.text = displayName;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : Color.clear;
        }

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;

        RefreshLights(Mathf.Max(1, slotCount), Mathf.Clamp(assignedCount, 0, slotCount));

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        _onSelect?.Invoke(_jobId);
    }

    private void RefreshLights(int slotCount, int assignedCount)
    {
        if (lightRoot == null)
            return;

        EnsureLightCount(slotCount);
        for (int i = 0; i < _lightInstances.Count; i++)
        {
            Image light = _lightInstances[i];
            if (light == null)
                continue;

            bool active = i < slotCount;
            light.gameObject.SetActive(active);
            if (!active)
                continue;

            bool on = i < assignedCount;
            Sprite stateSprite = on ? lightOnSprite : lightOffSprite;
            if (stateSprite != null)
                light.sprite = stateSprite;

            light.color = Color.white;
        }
    }

    private void EnsureLightCount(int count)
    {
        if (lightRoot == null)
            return;

        for (int i = _lightInstances.Count - 1; i >= 0; i--)
        {
            if (_lightInstances[i] == null)
                _lightInstances.RemoveAt(i);
        }

        if (lightPrefab != null)
        {
            while (_lightInstances.Count < count)
            {
                Image instance = Instantiate(lightPrefab, lightRoot);
                instance.gameObject.SetActive(true);
                _lightInstances.Add(instance);
            }

            return;
        }

        // Fallback: use existing children under lightRoot.
        for (int i = 0; i < lightRoot.childCount; i++)
        {
            Image childImage = lightRoot.GetChild(i).GetComponent<Image>();
            if (childImage == null)
                continue;
            if (!_lightInstances.Contains(childImage))
                _lightInstances.Add(childImage);
        }
    }
}
