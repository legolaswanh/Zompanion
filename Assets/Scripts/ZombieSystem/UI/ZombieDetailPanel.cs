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

        if (storyText != null)
            storyText.text = BuildStoryStatusText(definition, unlocked, storyUnlocked, zombieManager);

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
