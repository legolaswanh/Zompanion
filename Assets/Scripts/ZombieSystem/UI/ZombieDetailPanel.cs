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
    [SerializeField] private TMP_Text buffText;
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
            typeText.text = definition != null ? $"Type: {definition.Type} / {definition.Category}" : "Type: Unknown";

        if (stateText != null)
            stateText.text = $"State: {zombie.state}";

        if (buffText != null)
            buffText.text = $"Buff: {zombie.appliedBuffType} ({zombie.appliedBuffValue:0.##})";

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
            typeText.text = unlocked ? $"Type: {definition.Type} / {definition.Category}" : "Type: Locked";

        if (stateText != null)
        {
            if (!unlocked)
                stateText.text = "State: Locked";
            else
                stateText.text = following ? "State: Following" : "State: Unlocked";
        }

        if (buffText != null)
            buffText.text = unlocked ? $"Buff: {definition.BuffType} ({definition.BuffValue:0.##})" : "Buff: -";

        if (storyText != null)
            storyText.text = !unlocked ? "Story: Locked" : (storyUnlocked ? "Story: Unlocked" : "Story: Locked");

        if (detailPortraitImage != null)
        {
            Sprite target = definition.CodexDetailImage;
            if (target == null)
                target = GetBlackFallbackSprite();

            detailPortraitImage.sprite = target;
            detailPortraitImage.color = unlocked ? Color.white : Color.black;
        }
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
