using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Code.Scripts;

/// <summary>
/// 全局场景过渡管理器：在 Persistent 配置一次，所有「进入游戏后的」场景切换统一走过渡 UI + 加载进度。
/// 不依赖单独过渡场景，不依赖每场景配置。
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("过渡 UI（留空则自动生成简易黑底+进度条）")]
    [SerializeField] private GameObject loadingPanelPrefab;

    [Header("过渡时长")]
    [SerializeField] [Min(0f)] private float fadeInDuration = 0.3f;
    [SerializeField] [Min(0f)] private float fadeOutDuration = 0.4f;
    [Tooltip("过渡界面最少显示时间，避免一闪而过")]
    [SerializeField] [Min(0f)] private float minimumDisplayTime = 0.5f;
    [Tooltip("场景激活后继续显示过渡 UI 的时长（应 ≥ 黑幕淡出时间），避免先关 UI 再出现空白")]
    [SerializeField] [Min(0f)] private float postLoadDisplayTime = 1f;

    private GameObject _loadingPanel;
    private Image _progressBarFill;
    private CanvasGroup _panelCanvasGroup;
    private bool _isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureLoadingPanel();
        if (_loadingPanel != null)
            _loadingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureLoadingPanel()
    {
        if (_loadingPanel != null) return;

        if (loadingPanelPrefab != null)
        {
            _loadingPanel = Instantiate(loadingPanelPrefab, transform);
            var images = _loadingPanel.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name.Contains("Fill") || img.type == Image.Type.Filled) { _progressBarFill = img; break; }
                if (_progressBarFill == null) _progressBarFill = img;
            }
            if (_progressBarFill != null && _progressBarFill.type != Image.Type.Filled)
            {
                _progressBarFill.type = Image.Type.Filled;
                _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            }
            _panelCanvasGroup = _loadingPanel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
                _panelCanvasGroup = _loadingPanel.AddComponent<CanvasGroup>();
        }
        else
        {
            _loadingPanel = CreateDefaultLoadingPanel();
        }
    }

    private GameObject CreateDefaultLoadingPanel()
    {
        var root = new GameObject("SceneTransitionLoadingPanel");
        root.transform.SetParent(transform);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        _panelCanvasGroup = root.AddComponent<CanvasGroup>();
        _panelCanvasGroup.alpha = 0f;
        _panelCanvasGroup.blocksRaycasts = true;

        var bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var textGo = new GameObject("LoadingText");
        textGo.transform.SetParent(root.transform, false);
        var text = textGo.AddComponent<Text>();
        text.text = "Loading...";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.6f);
        textRect.anchorMax = new Vector2(0.5f, 0.6f);
        textRect.sizeDelta = new Vector2(400, 60);
        textRect.anchoredPosition = Vector2.zero;

        var sliderGo = new GameObject("ProgressBar");
        sliderGo.transform.SetParent(root.transform, false);
        var sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.25f, 0.4f);
        sliderRect.anchorMax = new Vector2(0.75f, 0.45f);
        sliderRect.offsetMin = sliderRect.offsetMax = Vector2.zero;
        var progressBg = new GameObject("Background").AddComponent<Image>();
        progressBg.transform.SetParent(sliderGo.transform, false);
        progressBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var progressBgRect = progressBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = Vector2.zero;
        progressBgRect.anchorMax = Vector2.one;
        progressBgRect.offsetMin = progressBgRect.offsetMax = Vector2.zero;
        var fillGo = new GameObject("Fill").AddComponent<Image>();
        fillGo.transform.SetParent(sliderGo.transform, false);
        fillGo.color = new Color(0.2f, 0.6f, 1f, 1f);
        fillGo.type = Image.Type.Filled;
        fillGo.fillMethod = Image.FillMethod.Horizontal;
        fillGo.fillAmount = 0f;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        _progressBarFill = fillGo;

        return root;
    }

    /// <summary>带过渡 UI 和进度条的加载；从 OpeningStory 进入游戏起，所有场景切换建议走此接口。</summary>
    public void LoadSceneWithTransition(string sceneName)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] 正在过渡中，忽略重复请求");
            return;
        }
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] 场景名为空");
            return;
        }
        StartCoroutine(LoadSceneWithTransitionCoroutine(sceneName));
    }

    private IEnumerator LoadSceneWithTransitionCoroutine(string sceneName)
    {
        _isTransitioning = true;
        EnsureLoadingPanel();
        if (_progressBarFill != null) _progressBarFill.fillAmount = 0f;
        if (_loadingPanel != null) _loadingPanel.SetActive(true);
        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;

        if (TransitionFadeManager.Instance != null && fadeInDuration > 0f)
        {
            TransitionFadeManager.Instance.SetBlack();
            yield return TransitionFadeManager.Instance.FadeIn(fadeInDuration);
        }

        var startTime = Time.unscaledTime;
        var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (asyncOp == null)
        {
            Debug.LogError("[SceneTransitionManager] LoadSceneAsync 失败: " + sceneName);
            _isTransitioning = false;
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
            yield break;
        }
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
        {
            if (_progressBarFill != null)
                _progressBarFill.fillAmount = Mathf.Clamp01(asyncOp.progress / 0.9f);
            yield return null;
        }
        if (_progressBarFill != null) _progressBarFill.fillAmount = 1f;

        var elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumDisplayTime)
            yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsed);

        // 必须在 allowSceneActivation 之前注册「下次场景加载时淡出」，否则 sceneLoaded 会先于本帧执行，淡出永远不会触发导致黑屏
        if (TransitionFadeManager.Instance != null)
        {
            TransitionFadeManager.Instance.SetBlack();
            TransitionFadeManager.Instance.FadeOutOnNextSceneLoad();
        }

        asyncOp.allowSceneActivation = true;
        yield return asyncOp; // 等待场景完全加载并激活

        // 继续显示过渡 UI，等黑幕淡出 + 场景首帧渲染完成后再关，避免出现空白画面（TransitionFadeManager 淡出约 0.8s）
        yield return new WaitForSecondsRealtime(postLoadDisplayTime);

        if (_loadingPanel != null) _loadingPanel.SetActive(false);
        _isTransitioning = false;
    }
}
