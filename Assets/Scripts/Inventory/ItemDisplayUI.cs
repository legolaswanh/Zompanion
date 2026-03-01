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

    private Action onWindowClosed;

    public void ShowItem(Sprite icon, string name, string description, Action callback)
    {
        itemIcon.sprite = icon;
        itemNameText.text = name;
        itemDescriptionText.text = description;
        onWindowClosed = callback;
        gameObject.SetActive(true);

        UIPauseState.RequestPause(this);
    }

    public void CloseWindow()
    {
        UIPauseState.ReleasePause(this);
        onWindowClosed?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        UIPauseState.ReleasePause(this);
    }
}
