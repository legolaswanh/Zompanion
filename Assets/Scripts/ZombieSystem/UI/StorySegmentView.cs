using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剧情段 UI 视图：物品图标、物品名称、剧情内容。支持解锁/未解锁状态显示。
/// 按约定子节点路径自动查找：ItemImage/image（图标）、ItemName（物品名）、Story（剧情文本）。
/// 无需手动绑定 itemIconImage、itemNameText。
/// </summary>
public class StorySegmentView : MonoBehaviour
{
    private const string LockedMessage = "Find key item to unlock the story";
    private const string PathItemIcon = "ItemImage/image";
    private const string PathItemName = "ItemName";
    private const string PathStory = "Story";

    [SerializeField] private Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private Image _itemIconImage;
    private TMP_Text _itemNameText;
    private TMP_Text _storyContentText;

    private void CacheReferences()
    {
        if (_itemIconImage != null)
            return;

        Transform t = transform.Find(PathItemIcon);
        _itemIconImage = t != null ? t.GetComponent<Image>() : null;

        t = transform.Find(PathItemName);
        _itemNameText = t != null ? t.GetComponent<TMP_Text>() : null;

        t = transform.Find(PathStory);
        _storyContentText = t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>
    /// 绑定剧情段。从 segment.requiredItem 读取物品图标和名称。未解锁时：黑图标、???、灰色提示；已解锁时：正常显示。
    /// </summary>
    public void Bind(ZombieStorySegmentConfig segment, bool zombieUnlocked, ZombieManager zombieManager)
    {
        bool hasSegment = segment != null && !string.IsNullOrWhiteSpace(segment.storyId);
        gameObject.SetActive(hasSegment);

        if (!hasSegment)
            return;

        CacheReferences();
        bool storyUnlocked = zombieUnlocked && zombieManager != null && zombieManager.IsStoryUnlocked(segment.storyId);

        // 物品图标（ItemImage/image）
        if (_itemIconImage != null)
        {
            Sprite icon = segment.requiredItem != null ? segment.requiredItem.icon : null;
            _itemIconImage.sprite = icon;
            _itemIconImage.color = icon != null ? (storyUnlocked ? Color.white : Color.black) : Color.clear;
        }

        // 物品名称（ItemName）
        if (_itemNameText != null)
        {
            _itemNameText.text = storyUnlocked && segment.requiredItem != null
                ? segment.requiredItem.itemName
                : "???";
            _itemNameText.color = storyUnlocked ? Color.black : lockedTextColor;
        }

        // 剧情内容（Story）
        if (_storyContentText != null)
        {
            _storyContentText.text = storyUnlocked
                ? (segment.storySummary ?? "")
                : LockedMessage;
            _storyContentText.color = storyUnlocked ? Color.black : lockedTextColor;
        }
    }

    /// <summary>
    /// 隐藏视图（用于无对应 segment 时）
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
