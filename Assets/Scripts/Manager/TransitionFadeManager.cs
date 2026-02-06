using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Code.Scripts
{
    /// <summary>
    /// 过渡淡入淡出管理器：跨场景存在，提供黑屏淡入淡出效果。
    /// 过渡场景：淡出黑屏显示文字 → 等待 → 淡入黑屏 → 加载目标场景 → 淡出黑屏显示新场景
    /// </summary>
    public class TransitionFadeManager : MonoBehaviour
    {
        public static TransitionFadeManager Instance { get; private set; }

        [SerializeField] private Color fadeColor = Color.black;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
        private bool _fadeInOnNextSceneLoad;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
            Debug.Log("[TransitionFadeManager] 已创建");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void CreateOverlay()
        {
            var go = new GameObject("TransitionFadeOverlay");
            go.transform.SetParent(transform);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

            _canvasGroup = go.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f; // 初始透明，不遮挡首场景
            _canvasGroup.blocksRaycasts = false;

            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(go.transform);
            var image = imageGO.AddComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = false;

            var rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            go.SetActive(true);
        }

        /// <summary>淡出黑屏（alpha 1→0），显示下层内容</summary>
        public IEnumerator FadeOut(float duration)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(1f, 0f, duration));
            yield return _fadeCoroutine;
            _fadeCoroutine = null;
        }

        /// <summary>淡入黑屏（alpha 0→1），覆盖内容</summary>
        public IEnumerator FadeIn(float duration)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(0f, 1f, duration));
            yield return _fadeCoroutine;
            _fadeCoroutine = null;
        }

        /// <summary>设置为黑屏（alpha=1）</summary>
        public void SetBlack()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        /// <summary>设置为透明（alpha=0）</summary>
        public void SetClear()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        /// <summary>下次场景加载时自动淡出黑屏</summary>
        public void FadeOutOnNextSceneLoad()
        {
            _fadeInOnNextSceneLoad = true;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f) { _canvasGroup.alpha = to; yield break; }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_fadeInOnNextSceneLoad) return;
            _fadeInOnNextSceneLoad = false;
            StartCoroutine(FadeOutAfterLoad());
        }

        private IEnumerator FadeOutAfterLoad()
        {
            yield return null; // 等一帧让新场景初始化
            yield return FadeOut(0.8f);
            Debug.Log("[TransitionFadeManager] 新场景淡入完成");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
