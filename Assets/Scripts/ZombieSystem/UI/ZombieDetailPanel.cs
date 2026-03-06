using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ZombieDetailPanel : MonoBehaviour
{
    private static Sprite _blackFallbackSprite;

    [Header("Fields")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text stateText;
    [FormerlySerializedAs("buffText")]
    [SerializeField] private TMP_Text modifierText;
    [SerializeField] private TMP_Text storyText;
    [Header("Info Page - Description (shortDescription)")]
    [Tooltip("背景故事，绑定 Detail_pannel 下的 Description")]
    [SerializeField] private TMP_Text storyBgText;
    [Tooltip("Description 的父节点，用于强制刷新布局。若为空则从 storyBgText 取 parent")]
    [SerializeField] private RectTransform storyContentRoot;

    [Header("Story Page - Segment Views")]
    [Tooltip("剧情页左半：Story_01")]
    [SerializeField] private StorySegmentView story01View;
    [Tooltip("剧情页右半：Story_02")]
    [SerializeField] private StorySegmentView story02View;
    [FormerlySerializedAs("portraitImage")]
    [SerializeField] private Image detailPortraitImage;

    public void Bind(ZombieInstanceData zombie, ZombieDefinitionSO definition, bool storyUnlocked)
    {
        if (zombie == null)
        {
            Clear();
            return;
        }

        SetDetailVisible(true);

        if (titleText != null)
            titleText.text = zombie.displayName;

        if (idText != null)
            idText.text = $"ID: {zombie.instanceId}";

        if (typeText != null)
            typeText.text = definition != null ? $"Category: {definition.Category}" : "Category: Unknown";

        if (stateText != null)
            stateText.text = $"State: {zombie.state}";

        if (modifierText != null)
            modifierText.text = $"Modifier: {zombie.appliedModifierType} ({zombie.appliedModifierValue:0.##})";

        if (storyText != null)
            storyText.text = storyUnlocked ? "Story: Unlocked" : "Story: Locked";

        if (detailPortraitImage != null)
        {
            Sprite target = definition != null ? definition.CodexDetailImage : null;
            if (target == null)
                target = GetBlackFallbackSprite();

            detailPortraitImage.sprite = target;
            detailPortraitImage.color = Color.white;
        }
    }

    public void Clear()
    {
        SetDetailVisible(false);
    }

    public void BindDefinition(ZombieDefinitionSO definition, bool unlocked, bool following, bool storyUnlocked)
    {
        BindDefinition(definition, unlocked, following, storyUnlocked, null);
    }

    /// <summary>
    /// 绑定僵尸图鉴详情。若传入 zombieManager，则按多段剧情展示每段解锁状态（如 Seg1 ✓、Seg2 🔒）。
    /// </summary>
    public void BindDefinition(ZombieDefinitionSO definition, bool unlocked, bool following, bool storyUnlocked, ZombieManager zombieManager)
    {
        if (definition == null)
        {
            Clear();
            return;
        }

        SetDetailVisible(true);

        if (titleText != null)
            titleText.text = unlocked ? definition.DisplayName : "???";

        if (idText != null)
            idText.text = $"ID: {definition.DefinitionId}";

        if (typeText != null)
            typeText.text = unlocked ? $"Category: {definition.Category}" : "Category: Locked";

        if (stateText != null)
        {
            if (!unlocked)
                stateText.text = "State: Locked";
            else
                stateText.text = following ? "State: Following" : "State: Unlocked";
        }

        if (modifierText != null)
            modifierText.text = unlocked
                ? $"Modifier: {definition.ModifierType} {definition.ModifierApplyMode} ({definition.ModifierValue:0.##})"
                : "Modifier: -";

        string statusText = BuildStoryStatusText(definition, unlocked, storyUnlocked, zombieManager);

        if (storyBgText != null)
        {
            string bg = unlocked ? (definition.ShortDescription ?? "") : "";
            storyBgText.text = bg;
            storyBgText.gameObject.SetActive(!string.IsNullOrEmpty(bg));
        }

        RefreshStoryLayout();

        if (storyText != null)
        {
            string contentText = BuildUnlockedStoryContent(definition, unlocked, zombieManager);
            storyText.text = string.IsNullOrEmpty(contentText)
                ? statusText
                : statusText + "\n\n" + contentText;
        }

        if (detailPortraitImage != null)
        {
            Sprite target = definition.CodexDetailImage;
            if (target == null)
                target = GetBlackFallbackSprite();

            detailPortraitImage.sprite = target;
            detailPortraitImage.color = unlocked ? Color.white : Color.black;
        }
    }

    private static string BuildStoryStatusText(ZombieDefinitionSO definition, bool unlocked, bool storyUnlocked, ZombieManager zombieManager)
    {
        if (!unlocked)
            return "Story: Locked";

        IReadOnlyList<ZombieStorySegmentConfig> segments = definition?.StorySegments;
        if (segments != null && segments.Count > 0 && zombieManager != null)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < segments.Count; i++)
            {
                ZombieStorySegmentConfig seg = segments[i];
                if (seg == null || string.IsNullOrWhiteSpace(seg.storyId))
                    continue;
                if (sb.Length > 0)
                    sb.Append("  ");
                bool unlockedSeg = zombieManager.IsStoryUnlocked(seg.storyId);
                sb.Append(unlockedSeg ? $"Seg{i + 1} ✓" : $"Seg{i + 1} 🔒");
            }
            return sb.Length > 0 ? "Story: " + sb : (storyUnlocked ? "Story: Unlocked" : "Story: Locked");
        }

        return storyUnlocked ? "Story: Unlocked" : "Story: Locked";
    }

    /// <summary>
    /// 绑定剧情页：Story_01、Story_02 的物品图标、名称、剧情内容。
    /// </summary>
    public void BindStoryPage(ZombieDefinitionSO definition, bool zombieUnlocked, ZombieManager zombieManager)
    {
        if (definition == null)
        {
            if (story01View != null) story01View.Hide();
            if (story02View != null) story02View.Hide();
            return;
        }

        IReadOnlyList<ZombieStorySegmentConfig> segments = definition.StorySegments;
        if (segments == null || segments.Count == 0)
        {
            if (story01View != null) story01View.Hide();
            if (story02View != null) story02View.Hide();
            return;
        }

        if (story01View != null)
        {
            var seg0 = segments.Count > 0 ? segments[0] : null;
            story01View.Bind(seg0, zombieUnlocked, zombieManager);
        }

        if (story02View != null)
        {
            var seg1 = segments.Count > 1 ? segments[1] : null;
            story02View.Bind(seg1, zombieUnlocked, zombieManager);
        }
    }

    private void RefreshStoryLayout()
    {
        RectTransform root = storyContentRoot;
        if (root == null && storyBgText != null)
            root = storyBgText.rectTransform.parent as RectTransform;
        if (root == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private static string BuildUnlockedStoryContent(ZombieDefinitionSO definition, bool unlocked, ZombieManager zombieManager)
    {
        if (!unlocked || definition == null || zombieManager == null)
            return "";

        var sb = new StringBuilder();

        // 背景故事（shortDescription）
        if (!string.IsNullOrWhiteSpace(definition.ShortDescription))
            sb.Append(definition.ShortDescription);

        // 已解锁剧情段落摘要
        IReadOnlyList<ZombieStorySegmentConfig> segments = definition.StorySegments;
        if (segments != null && segments.Count > 0)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                ZombieStorySegmentConfig seg = segments[i];
                if (seg == null || string.IsNullOrWhiteSpace(seg.storyId))
                    continue;
                if (!zombieManager.IsStoryUnlocked(seg.storyId))
                    continue;
                if (!string.IsNullOrWhiteSpace(seg.storySummary))
                {
                    if (sb.Length > 0)
                        sb.Append("\n\n");
                    sb.Append(seg.storySummary);
                }
            }
        }
        return sb.ToString();
    }

    private static Sprite GetBlackFallbackSprite()
    {
        if (_blackFallbackSprite != null)
            return _blackFallbackSprite;

        Texture2D texture = Texture2D.blackTexture;
        _blackFallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _blackFallbackSprite;
    }

    private void SetDetailVisible(bool visible)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
                child.gameObject.SetActive(visible);
        }
    }
}
