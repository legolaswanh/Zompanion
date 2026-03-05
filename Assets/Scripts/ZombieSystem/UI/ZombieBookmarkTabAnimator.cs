using UnityEngine;
using UnityEngine.EventSystems;

public class ZombieBookmarkTabAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private bool useCurrentXAsNormal = true;
    [SerializeField] private float normalX = 0f;
    [SerializeField] private float activeOffsetX = -24f;
    [SerializeField] private float moveDuration = 0.12f;

    private bool _selected;
    private bool _hovered;
    private Coroutine _moveCoroutine;

    private void Awake()
    {
        if (targetRect == null)
            targetRect = transform as RectTransform;

        if (targetRect != null && useCurrentXAsNormal)
            normalX = targetRect.anchoredPosition.x;
    }

    private void OnEnable()
    {
        ApplyStateInstant();
    }

    public void SetSelected(bool selected, bool instant = false)
    {
        _selected = selected;
        RefreshPosition(instant);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        RefreshPosition(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        RefreshPosition(false);
    }

    private void ApplyStateInstant()
    {
        if (targetRect == null)
            return;

        Vector2 pos = targetRect.anchoredPosition;
        pos.x = GetTargetX();
        targetRect.anchoredPosition = pos;
    }

    private void RefreshPosition(bool instant)
    {
        if (targetRect == null)
            return;

        float targetX = GetTargetX();
        if (instant || moveDuration <= 0f)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            Vector2 pos = targetRect.anchoredPosition;
            pos.x = targetX;
            targetRect.anchoredPosition = pos;
            return;
        }

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveToX(targetX));
    }

    private System.Collections.IEnumerator MoveToX(float targetX)
    {
        float elapsed = 0f;
        Vector2 start = targetRect.anchoredPosition;
        Vector2 end = start;
        end.x = targetX;

        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            targetRect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        targetRect.anchoredPosition = end;
        _moveCoroutine = null;
    }

    private float GetTargetX()
    {
        return (_selected || _hovered) ? normalX + activeOffsetX : normalX;
    }
}
