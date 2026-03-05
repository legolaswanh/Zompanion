using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SummonCanvas : MonoBehaviour
{
    public event Action<bool> OnMiniGameCompleted;

    public GameObject linePrefab;
    public GameObject fuzhiPaper;

    [Header("UI Overlay")]
    [SerializeField] private Image backgroundDimmer;
    [SerializeField] private Text guideText;
    [SerializeField, Range(0f, 1f)] private float dimmerAlpha = 0.55f;
    [SerializeField] private float dimmerFadeDuration = 0.2f;
    [SerializeField] private float guideFadeDuration = 0.2f;
    [SerializeField] private string guideMessage = "Draw a talisman in 1 stroke";
    
    private LineRenderer currentLine;
    private List<GameObject> allLines = new List<GameObject>();
    private List<Vector3> fingerPositions = new List<Vector3>();
    private bool canDraw = false;
    private bool isDrawing = false;
    private bool hasDrawnStroke = false;
    
    [SerializeField] private float autoFinishDelay = 1.5f;
    [SerializeField] private float appearDuration = 1f;
    [SerializeField] private float lineGlowTarget = 3f;
    [SerializeField] private float shakeAmplitude = 0.1f;
    [SerializeField] private float explodeScaleMultiplier = 1.3f;
    [SerializeField] private float explodeDuration = 0.25f;
    
    private Color paperOriginalColor = Color.white;
    private Vector3 paperBaseScale = Vector3.one;
    private Color dimmerOriginalColor = Color.black;
    private Color guideOriginalColor = Color.white;

    private bool _initialized;

    private void InitPaper()
    {
        if (_initialized || fuzhiPaper == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        // 符纸放在距离相机 1f 的位置
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane + 1f);
        fuzhiPaper.transform.position = cam.ScreenToWorldPoint(screenCenter);

        var sr = fuzhiPaper.GetComponent<SpriteRenderer>();
        var img = fuzhiPaper.GetComponent<Image>();
        if (sr != null) 
        {
            paperOriginalColor = sr.color;
            sr.sortingOrder = 20000; // 修复点：强制符纸有一个较高的排序层级
        }
        else if (img != null) 
        {
            paperOriginalColor = img.color;
        }

        paperBaseScale = fuzhiPaper.transform.localScale;
        if (paperBaseScale == Vector3.zero)
            paperBaseScale = Vector3.one;

        fuzhiPaper.SetActive(false);
        _initialized = true;

        EnsureOverlayUI();
        if (backgroundDimmer != null)
        {
            dimmerOriginalColor = backgroundDimmer.color;
            SetImageAlpha(backgroundDimmer, 0f);
            backgroundDimmer.gameObject.SetActive(false);
            // 修复点：无论它是预设的还是生成的，强制它排在 UI 的最底层（最先渲染）
            backgroundDimmer.transform.SetAsFirstSibling();
        }
        if (guideText != null)
        {
            guideOriginalColor = guideText.color;
            guideText.text = guideMessage;
            SetTextAlpha(guideText, 0f);
            guideText.gameObject.SetActive(false);
            // 强制文字在 UI 的最上层
            guideText.transform.SetAsLastSibling();
        }
    }

    void Awake()
    {
        InitPaper();
    }

    void Update()
    {
        if (!canDraw) return;

        if (Input.GetMouseButtonDown(0) && !hasDrawnStroke)
        {
            isDrawing = true;
            CreateLine();
        }
        if (Input.GetMouseButton(0) && isDrawing && currentLine != null)
        {
            Vector3 screenPos = Input.mousePosition;
            // 修复点：线条的 Z 深度设为 0.5f，比符纸（1f）更靠近相机，防止被遮挡
            screenPos.z = Camera.main.nearClipPlane + 0.5f;
            Vector3 tempFingerPos = Camera.main.ScreenToWorldPoint(screenPos);
            if (fingerPositions.Count > 0 &&
                Vector3.Distance(tempFingerPos, fingerPositions[fingerPositions.Count - 1]) > 0.1f)
            {
                UpdateLine(tempFingerPos);
            }
        }
        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            hasDrawnStroke = true;
            StartCoroutine(AutoFinishAfterDelay());
        }
    }

    public void StartDraw()
    {
        InitPaper();
        canDraw = true;
        hasDrawnStroke = false;
        if (fuzhiPaper != null)
        {
            StopAllCoroutines();
            EnsureOverlayUI();
            StartCoroutine(PlayStartUISequence());
            fuzhiPaper.SetActive(true);

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane + 1f);
                fuzhiPaper.transform.position = cam.ScreenToWorldPoint(screenCenter);
            }

            var sr = fuzhiPaper.GetComponent<SpriteRenderer>();
            var img = fuzhiPaper.GetComponent<Image>();
            if (sr != null)
                sr.color = paperOriginalColor;
            else if (img != null)
                img.color = paperOriginalColor;

            fuzhiPaper.transform.localScale = Vector3.zero;
            StartCoroutine(PlayFuzhiAppear());
        }
    }

    void CreateLine()
    {
        GameObject newLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        currentLine = newLine.GetComponent<LineRenderer>();
        // 修复点：赋予超高的 sortingOrder 强制其在绝大多数对象之上渲染
        currentLine.sortingOrder = 30000; 
        
        allLines.Add(newLine);
        fingerPositions.Clear();
        Vector3 screenPos = Input.mousePosition;
        // 修复点：线条 Z 深度更近
        screenPos.z = Camera.main.nearClipPlane + 0.5f; 
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
        fingerPositions.Add(mousePos);
        fingerPositions.Add(mousePos);
        currentLine.positionCount = 2;
        currentLine.SetPosition(0, fingerPositions[0]);
        currentLine.SetPosition(1, fingerPositions[1]);
    }

    void UpdateLine(Vector3 newFingerPos)
    {
        fingerPositions.Add(newFingerPos);
        currentLine.positionCount++;
        currentLine.SetPosition(currentLine.positionCount - 1, newFingerPos);
    }

    public void ConfirmSummon()
    {
        canDraw = false;
        if (fuzhiPaper != null) fuzhiPaper.SetActive(false);
        if (backgroundDimmer != null) backgroundDimmer.gameObject.SetActive(false);
        if (guideText != null) guideText.gameObject.SetActive(false);
        ClearCanvas();
        OnMiniGameCompleted?.Invoke(true);
    }

    public void ClearCanvas()
    {
        foreach (var line in allLines) Destroy(line);
        allLines.Clear();
    }

    private IEnumerator AutoFinishAfterDelay()
    {
        float duration = autoFinishDelay;
        float t = 0f;

        Vector3 paperStartLocalPos = Vector3.zero;
        Vector3 paperStartScale = Vector3.one;
        SpriteRenderer paperSr = null;
        Image paperImg = null;
        Color paperColor = paperOriginalColor;
        Vector3 explodeCenter = Vector3.zero;

        if (fuzhiPaper != null)
        {
            paperStartLocalPos = fuzhiPaper.transform.localPosition;
            paperStartScale = fuzhiPaper.transform.localScale;
            explodeCenter = fuzhiPaper.transform.position;
            paperSr = fuzhiPaper.GetComponent<SpriteRenderer>();
            paperImg = fuzhiPaper.GetComponent<Image>();
            if (paperSr != null) paperColor = paperSr.color;
            else if (paperImg != null) paperColor = paperImg.color;
        }

        float initialGlow = 0f;
        var lineOriginalPositions = new Dictionary<LineRenderer, Vector3[]>();

        foreach (var line in allLines)
        {
            var lr = line != null ? line.GetComponent<LineRenderer>() : null;
            if (lr == null) continue;

            if (lr.material != null && lr.material.HasProperty("_GlowIntensity"))
            {
                initialGlow = lr.material.GetFloat("_GlowIntensity");
            }

            int count = lr.positionCount;
            var arr = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = lr.GetPosition(i);
            }
            lineOriginalPositions[lr] = arr;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            float ease = 1f - (1f - lerp) * (1f - lerp);

            foreach (var kv in lineOriginalPositions)
            {
                var lr = kv.Key;
                var orig = kv.Value;
                if (lr == null) continue;

                if (lr.material != null && lr.material.HasProperty("_GlowIntensity"))
                {
                    lr.material.SetFloat("_GlowIntensity", Mathf.Lerp(initialGlow, lineGlowTarget, ease));
                }

                Vector2 offset2D = UnityEngine.Random.insideUnitCircle * shakeAmplitude * ease;
                Vector3 offset3D = new Vector3(offset2D.x, offset2D.y, 0f);
                for (int i = 0; i < orig.Length; i++)
                {
                    lr.SetPosition(i, orig[i] + offset3D);
                }
            }

            if (fuzhiPaper != null)
            {
                Vector2 offset2D = UnityEngine.Random.insideUnitCircle * shakeAmplitude * ease;
                fuzhiPaper.transform.localPosition =
                    paperStartLocalPos + new Vector3(offset2D.x, offset2D.y, 0f);
            }

            yield return null;
        }

        if (fuzhiPaper != null)
        {
            float et = 0f;
            while (et < explodeDuration)
            {
                et += Time.deltaTime;
                float lerp = Mathf.Clamp01(et / explodeDuration);
                float ease = 1f - (1f - lerp) * (1f - lerp);

                float scale = Mathf.Lerp(1f, explodeScaleMultiplier, ease);
                fuzhiPaper.transform.localScale = paperStartScale * scale;

                foreach (var kv in lineOriginalPositions)
                {
                    var lr = kv.Key;
                    var orig = kv.Value;
                    if (lr == null) continue;

                    for (int i = 0; i < orig.Length; i++)
                    {
                        Vector3 dir = orig[i] - explodeCenter;
                        lr.SetPosition(i, explodeCenter + dir * scale);
                    }
                }

                float alpha = Mathf.Lerp(paperColor.a, 0f, ease);
                if (paperSr != null)
                {
                    var c = paperSr.color;
                    c.a = alpha;
                    paperSr.color = c;
                }
                else if (paperImg != null)
                {
                    var c = paperImg.color;
                    c.a = alpha;
                    paperImg.color = c;
                }

                yield return null;
            }

            fuzhiPaper.transform.localPosition = paperStartLocalPos;
            fuzhiPaper.transform.localScale = paperStartScale;
        }

        ConfirmSummon();
    }

    private IEnumerator PlayFuzhiAppear()
    {
        if (fuzhiPaper == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = paperBaseScale;
        float t = 0f;

        while (t < appearDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / appearDuration);
            lerp = 1f - (1f - lerp) * (1f - lerp);
            fuzhiPaper.transform.localScale = Vector3.Lerp(startScale, endScale, lerp);
            yield return null;
        }

        fuzhiPaper.transform.localScale = endScale;
    }

    private IEnumerator PlayStartUISequence()
    {
        if (backgroundDimmer != null)
        {
            backgroundDimmer.gameObject.SetActive(true);
            SetImageAlpha(backgroundDimmer, 0f);
            yield return FadeImage(backgroundDimmer, dimmerAlpha, dimmerFadeDuration);
        }

        if (appearDuration > 0f) yield return new WaitForSeconds(appearDuration);

        if (guideText != null)
        {
            guideText.gameObject.SetActive(true);
            guideText.text = guideMessage;
            SetTextAlpha(guideText, 0f);
            yield return FadeText(guideText, guideOriginalColor.a, guideFadeDuration);
        }
    }

    private void EnsureOverlayUI()
    {
        if (backgroundDimmer != null && guideText != null) return;
        if (fuzhiPaper == null) return;

        var canvas = fuzhiPaper.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // 全屏遮罩
        if (backgroundDimmer == null)
        {
            var go = new GameObject("SummonDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            backgroundDimmer = go.GetComponent<Image>();
            backgroundDimmer.raycastTarget = false;
            backgroundDimmer.color = new Color(0f, 0f, 0f, 0f);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 修复点：强制设为第一个子节点，渲染在最低层
            go.transform.SetAsFirstSibling();
        }

        // 引导文字
        if (guideText == null)
        {
            var go = new GameObject("SummonGuideText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            guideText = go.GetComponent<Text>();
            guideText.raycastTarget = false;
            guideText.alignment = TextAnchor.UpperCenter;
            var builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (builtInFont != null) guideText.font = builtInFont;
            guideText.fontSize = 32;
            guideText.color = new Color(1f, 1f, 1f, 1f);
            guideText.text = guideMessage;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(1200f, 120f);

            go.transform.SetAsLastSibling();
        }
    }

    private static IEnumerator FadeImage(Image img, float targetAlpha, float duration)
    {
        if (img == null) yield break;
        if (duration <= 0f) { SetImageAlpha(img, targetAlpha); yield break; }

        float start = img.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
            SetImageAlpha(img, a);
            yield return null;
        }
        SetImageAlpha(img, targetAlpha);
    }

    private static IEnumerator FadeText(Text txt, float targetAlpha, float duration)
    {
        if (txt == null) yield break;
        if (duration <= 0f) { SetTextAlpha(txt, targetAlpha); yield break; }

        float start = txt.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
            SetTextAlpha(txt, a);
            yield return null;
        }
        SetTextAlpha(txt, targetAlpha);
    }

    private static void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private static void SetTextAlpha(Text txt, float alpha)
    {
        if (txt == null) return;
        var c = txt.color;
        c.a = alpha;
        txt.color = c;
    }
}