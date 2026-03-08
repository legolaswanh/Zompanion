using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 控制 Canvas 上的 HintPanel，用于显示挖掘等操作时的提示文本（如背包满了）。
/// </summary>
public class HintPanelController : MonoBehaviour
{
    public static HintPanelController Instance { get; private set; }

    /// <summary>
    /// 获取实例，若 Instance 为空则尝试 FindObjectOfType（用于场景加载顺序导致的未初始化情况）。
    /// </summary>
    public static HintPanelController GetInstance()
    {
        if (Instance != null)
            return Instance;
        return UnityEngine.Object.FindObjectOfType<HintPanelController>(true);
    }

    [Header("References")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTextTMP;

    [Header("Config")]
    [SerializeField] private float defaultDuration = 2f;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        if (hintPanel == null)
            hintPanel = transform.Find("HintPanel")?.gameObject ?? gameObject;
        if (hintTextTMP == null)
            hintTextTMP = GetComponentInChildren<TextMeshProUGUI>(true);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 显示提示文本，默认 2 秒后自动隐藏。
    /// </summary>
    public void ShowHint(string text, float duration = -1f)
    {
        if (string.IsNullOrEmpty(text))
            return;

        SetHintText(text);
        if (hintPanel != null)
            hintPanel.SetActive(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        float d = duration > 0 ? duration : defaultDuration;
        _hideCoroutine = StartCoroutine(HideAfter(d));
    }

    /// <summary>
    /// 立即隐藏提示。
    /// </summary>
    public void Hide()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private void SetHintText(string text)
    {
        if (hintTextTMP != null)
            hintTextTMP.text = text;
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        _hideCoroutine = null;
        Hide();
    }
}
