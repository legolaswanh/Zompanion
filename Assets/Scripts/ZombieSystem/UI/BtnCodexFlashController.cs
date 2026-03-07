using System.Collections;
using UnityEngine;

/// <summary>
/// BtnCodex 闪烁控制器。当僵尸寄回物品时，按钮以放大缩小方式闪烁提示玩家。
/// </summary>
public class BtnCodexFlashController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] [Min(0.01f)] private float flashDuration = 0.4f;
    [SerializeField] [Min(1f)] private float maxScale = 1.2f;
    [SerializeField] [Min(0.01f)] private float minScale = 0.95f;

    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Coroutine _flashCoroutine;
    private bool _subscribed;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _baseScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed || ZombieExploreService.Instance == null)
            return;
        ZombieExploreService.Instance.OnGiftArrived += OnGiftArrived;
        ZombieExploreService.Instance.OnGiftClaimed += OnGiftClaimed;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && ZombieExploreService.Instance != null)
        {
            ZombieExploreService.Instance.OnGiftArrived -= OnGiftArrived;
            ZombieExploreService.Instance.OnGiftClaimed -= OnGiftClaimed;
            _subscribed = false;
        }

        StopFlash();
    }

    private void OnGiftArrived(ItemDataSO item, string definitionId)
    {
        StartFlash();
    }

    private void OnGiftClaimed()
    {
        StopFlash();
    }

    private void StartFlash()
    {
        StopFlash();
        _flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private void StopFlash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;
    }

    private IEnumerator FlashCoroutine()
    {
        if (_rectTransform == null)
            yield break;

        float elapsed = 0f;
        bool scalingUp = true;

        while (ZombieExploreService.Instance != null && ZombieExploreService.Instance.HasPendingGift)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashDuration;

            if (t >= 1f)
            {
                elapsed = 0f;
                scalingUp = !scalingUp;
                t = 0f;
            }

            float from = scalingUp ? minScale : maxScale;
            float to = scalingUp ? maxScale : minScale;
            float scale = Mathf.Lerp(from, to, t);
            _rectTransform.localScale = _baseScale * scale;

            yield return null;
        }

        _rectTransform.localScale = _baseScale;
        _flashCoroutine = null;
    }
}
