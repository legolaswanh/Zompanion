using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI useInfoText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        Vector2 mousePos = Input.mousePosition;
        // 稍微偏移一点鼠标位置，防止 UI 挡住指针本身
        tooltipRect.position = mousePos + new Vector2(10, -10);
    }

    public void ShowTooltip(Sprite icon, string name, string info)
    {
        itemIcon.sprite = icon;
        itemNameText.text = name;
        useInfoText.text = info;
        canvasGroup.alpha = 1;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
    }
}