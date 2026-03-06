using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将角色的 RectTransform 水平位置与进度条 Fill 的 fillAmount 同步，
/// 实现「角色向右行走 = 加载进度」的视觉效果。
/// 挂在行走角色物体上，并配置进度来源（Fill Image 或 Track RectTransform）。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WalkingProgressIndicator : MonoBehaviour
{
    [Header("进度来源（二选一）")]
    [Tooltip("作为进度源的 Image（SceneTransitionManager 会自动更新其 fillAmount）")]
    [SerializeField] private Image progressFillImage;
    [Tooltip("若无 Fill 引用，会尝试在父物体下查找名为 Fill 的 Image")]
    [SerializeField] private bool autoFindFill = true;

    [Header("轨道范围")]
    [Tooltip("角色行走的轨道 RectTransform，用于计算左右边界；留空则使用父物体")]
    [SerializeField] private RectTransform trackRect;

    [Header("位置映射")]
    [Tooltip("进度 0 时角色的本地 X 位置（轨道左端）")]
    [SerializeField] private float startX = -400f;
    [Tooltip("进度 1 时角色的本地 X 位置（轨道右端）")]
    [SerializeField] private float endX = 400f;

    private RectTransform _rectTransform;
    private float _lastProgress = -1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (progressFillImage == null && autoFindFill)
        {
            var parent = transform.parent;
            if (parent != null)
            {
                var images = parent.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.name.Contains("Fill") || img.type == Image.Type.Filled)
                    {
                        progressFillImage = img;
                        break;
                    }
                }
            }
        }
        if (trackRect == null && transform.parent != null)
            trackRect = transform.parent as RectTransform;
    }

    private void Update()
    {
        float progress = GetProgress();
        if (progress < 0f) return;

        if (Mathf.Approximately(progress, _lastProgress)) return;
        _lastProgress = progress;

        ApplyProgress(progress);
    }

    private float GetProgress()
    {
        if (progressFillImage != null)
            return progressFillImage.fillAmount;

        return -1f;
    }

    private void ApplyProgress(float progress)
    {
        float x = Mathf.Lerp(startX, endX, Mathf.Clamp01(progress));
        var pos = _rectTransform.anchoredPosition;
        pos.x = x;
        _rectTransform.anchoredPosition = pos;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (trackRect != null)
        {
            var rect = trackRect.rect;
            if (startX == -400f && endX == 400f)
            {
                startX = -rect.width * 0.5f;
                endX = rect.width * 0.5f;
            }
        }
    }
#endif
}
