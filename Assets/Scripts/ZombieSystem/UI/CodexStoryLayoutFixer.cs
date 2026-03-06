using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 修复剧情滚动区域的布局：确保文字按实际高度排列、顶格显示、支持滚动。
/// 挂到 ZombieCodexPanel 或 ScrollStory 所在物体上，Awake 时自动修正。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CodexStoryLayoutFixer : MonoBehaviour
{
    [Tooltip("ScrollStory 物体（含 Scroll Rect）。若为空则从子物体查找")]
    [SerializeField] private RectTransform scrollStoryRoot;

    private void Awake()
    {
        ApplyFix();
    }

    private void OnEnable()
    {
        ApplyFix();
    }

    [ContextMenu("Apply Layout Fix")]
    public void ApplyFix()
    {
        RectTransform root = scrollStoryRoot != null ? scrollStoryRoot : (transform as RectTransform);
        if (root == null) return;

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        if (scrollRect == null) return;

        RectTransform viewport = scrollRect.viewport;
        RectTransform content = scrollRect.content;
        if (viewport == null || content == null) return;

        // 1. Content 锚点改为顶部，内容从上往下排，方便垂直滚动
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        // 2. Content Size Fitter：高度随子内容
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. Vertical Layout Group：子项按实际高度排列，顶格无内边距
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 8f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // 4. Scroll Rect：确保垂直滚动、提高灵敏度
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 25f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
