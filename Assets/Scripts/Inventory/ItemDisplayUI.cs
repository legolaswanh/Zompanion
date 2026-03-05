using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private bool allowKeyboardClose = true;

    private Action onWindowClosed;
    private int ignoreCloseInputUntilFrame = -1;

    public void ShowItem(Sprite icon, string name, string description, Action callback)
    {
        if (itemIcon != null)
            itemIcon.sprite = icon;

        if (itemNameText != null)
            itemNameText.text = name;

        if (itemDescriptionText != null)
            itemDescriptionText.text = description;

        onWindowClosed = callback;
        gameObject.SetActive(true);
        // Prevent the same key press that opened this window from immediately closing it.
        ignoreCloseInputUntilFrame = Time.frameCount;

        UIPauseState.RequestPause(this);
    }

    public void CloseWindow()
    {
        UIPauseState.ReleasePause(this);
        onWindowClosed?.Invoke();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!allowKeyboardClose || !gameObject.activeInHierarchy)
            return;

        if (Time.frameCount <= ignoreCloseInputUntilFrame)
            return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            CloseWindow();
    }

    private void OnDestroy()
    {
        UIPauseState.ReleasePause(this);
    }
}
