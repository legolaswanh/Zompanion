using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在 AssemblePlatformCanvas 上，负责：
/// 1. 打开时 Button 闪烁（缩放）效果
/// 2. ESC 关闭界面（通过 UIPanelCoordinator）
/// 3. 关闭时恢复玩家移动
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AssemblePlatformCanvasController : MonoBehaviour
{
    [Header("Assemble Button")]
    [SerializeField] private Button assembleButton;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float maxScale = 1.15f;
    [SerializeField] private float minScale = 0.9f;

    private RectTransform _buttonRect;
    private Vector3 _baseScale;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        if (assembleButton == null)
            assembleButton = GetComponentInChildren<Button>();
        if (assembleButton != null)
        {
            _buttonRect = assembleButton.GetComponent<RectTransform>();
            _baseScale = _buttonRect != null ? _buttonRect.localScale : Vector3.one;
        }
    }

    private void OnEnable()
    {
        UIPanelCoordinator.Register(gameObject);
        StartButtonFlash();
    }

    private void OnDisable()
    {
        StopButtonFlash();
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.EnableMove();
    }

    private void StartButtonFlash()
    {
        if (_buttonRect == null)
            return;

        StopButtonFlash();
        _flashCoroutine = StartCoroutine(ButtonFlashCoroutine());
    }

    private void StopButtonFlash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (_buttonRect != null)
            _buttonRect.localScale = _baseScale;
    }

    /// <summary>
    /// 单次闪烁：从小放大再缩回，播放一次后停止。
    /// </summary>
    private IEnumerator ButtonFlashCoroutine()
    {
        if (_buttonRect == null)
            yield break;

        float elapsed = 0f;
        const float halfDuration = 0.25f; // 前半段放大，后半段缩回

        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashDuration;

            float scale;
            if (t < 0.5f)
            {
                // 0 -> 0.5: minScale -> maxScale
                float u = t * 2f;
                scale = Mathf.Lerp(minScale, maxScale, u);
            }
            else
            {
                // 0.5 -> 1: maxScale -> 1
                float u = (t - 0.5f) * 2f;
                scale = Mathf.Lerp(maxScale, 1f, u);
            }

            _buttonRect.localScale = _baseScale * scale;
            yield return null;
        }

        _buttonRect.localScale = _baseScale;
        _flashCoroutine = null;
    }
}
