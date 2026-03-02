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

    private bool _initialized;

    private void InitPaper()
    {
        if (_initialized || fuzhiPaper == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane + 1f);
        fuzhiPaper.transform.position = cam.ScreenToWorldPoint(screenCenter);

        var sr = fuzhiPaper.GetComponent<SpriteRenderer>();
        var img = fuzhiPaper.GetComponent<Image>();
        if (sr != null) paperOriginalColor = sr.color;
        else if (img != null) paperOriginalColor = img.color;

        paperBaseScale = fuzhiPaper.transform.localScale;
        if (paperBaseScale == Vector3.zero)
            paperBaseScale = Vector3.one;

        fuzhiPaper.SetActive(false);
        _initialized = true;
        Debug.Log($"[SummonCanvas] InitPaper 完成 | pos={fuzhiPaper.transform.position} baseScale={paperBaseScale}");
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
            screenPos.z = Camera.main.nearClipPlane + 1f;
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

            Debug.Log($"[SummonCanvas] StartDraw | fuzhiPaper.active={fuzhiPaper.activeSelf} pos={fuzhiPaper.transform.position} targetScale={paperBaseScale}");
        }
        else
        {
            Debug.LogWarning("[SummonCanvas] StartDraw: fuzhiPaper 为 null");
        }
    }

    void CreateLine()
    {
        GameObject newLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        currentLine = newLine.GetComponent<LineRenderer>();
        allLines.Add(newLine);
        fingerPositions.Clear();
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Camera.main.nearClipPlane + 1f;
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
        if (fuzhiPaper != null)
        {
            fuzhiPaper.SetActive(false);
        }
        ClearCanvas();
        gameObject.SetActive(false);
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
}
