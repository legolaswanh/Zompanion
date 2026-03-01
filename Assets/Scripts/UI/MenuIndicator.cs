using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单角标指示器：鼠标悬停按钮时，角标移动到对应按钮的 Y 位置并显示。
/// 挂在角标 GameObject 上，将菜单按钮拖入 buttons 数组即可。
/// </summary>
public class MenuIndicator : MonoBehaviour
{
    [Header("需要响应的菜单按钮")]
    [SerializeField] private Button[] buttons;

    private RectTransform _indicatorRect;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _indicatorRect = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);
        RegisterButtons();
    }

    void RegisterButtons()
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();

            AddEntry(trigger, EventTriggerType.PointerEnter, _ => ShowAt(btn));
            AddEntry(trigger, EventTriggerType.PointerExit, _ => SetVisible(false));
        }
    }

    void AddEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        foreach (var existing in trigger.triggers)
        {
            if (existing.eventID == type) return;
        }

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    void ShowAt(Button btn)
    {
        if (!btn.interactable) return;

        var btnRect = btn.GetComponent<RectTransform>();
        var pos = _indicatorRect.anchoredPosition;
        pos.y = btnRect.anchoredPosition.y;
        _indicatorRect.anchoredPosition = pos;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
    }
}
