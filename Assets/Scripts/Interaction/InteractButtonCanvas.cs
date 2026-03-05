using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载在 buttonCanvas 上，负责交互按钮的闪烁效果。
/// 对 Image 子物体做 alpha 闪烁；若无 Image 则使用 CanvasGroup。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class InteractButtonCanvas : MonoBehaviour
{
    [Header("闪烁效果")]
    [SerializeField] [Range(0.2f, 2f)] private float flickerSpeed = 1f;
    [SerializeField] [Range(0f, 1f)] private float minAlpha = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float maxAlpha = 1f;

    private Image _targetImage;
    private CanvasGroup _canvasGroup;
    private bool _useImage;
    private float _time;

    private void Awake()
    {
        _targetImage = GetComponentInChildren<Image>(true);
        _useImage = _targetImage != null;
        if (!_useImage)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        _time = 0f;
    }

    private void Update()
    {
        _time += Time.deltaTime * flickerSpeed;
        float t = (Mathf.Sin(_time * Mathf.PI) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        if (_useImage && _targetImage != null)
        {
            Color c = _targetImage.color;
            c.a = alpha;
            _targetImage.color = c;
        }
        else if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }
    }
}
