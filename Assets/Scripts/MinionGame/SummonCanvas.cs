using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SummonCanvas : MonoBehaviour
{
    public GameObject linePrefab; // ?? LineRenderer ???
    public GameObject fuzhiPaper; // ????
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

    void Start()
    {
        // ???????????????????
        if (fuzhiPaper != null && Camera.main != null)
        {
            var cam = Camera.main;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane + 1f);
            Vector3 worldPos = cam.ScreenToWorldPoint(screenCenter);
            fuzhiPaper.transform.position = worldPos;
            // 记录初始颜色和缩放（作为“正常尺寸”）
            var sr = fuzhiPaper.GetComponent<SpriteRenderer>();
            var img = fuzhiPaper.GetComponent<Image>();
            if (sr != null) paperOriginalColor = sr.color;
            else if (img != null) paperOriginalColor = img.color;
            paperBaseScale = fuzhiPaper.transform.localScale;
            fuzhiPaper.SetActive(false);
        }
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
            screenPos.z = Camera.main.nearClipPlane + 1f; // ???????
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
        canDraw = true;
        hasDrawnStroke = false;
        if (fuzhiPaper != null)
        {
            // 符纸从小到大出现
            StopAllCoroutines();
            fuzhiPaper.SetActive(true);
            // 恢复颜色为初始值，避免上次淡出后为 0 alpha
            var sr = fuzhiPaper.GetComponent<SpriteRenderer>();
            var img = fuzhiPaper.GetComponent<Image>();
            if (sr != null)
            {
                var c = paperOriginalColor;
                sr.color = c;
            }
            else if (img != null)
            {
                var c = paperOriginalColor;
                img.color = c;
            }
            // 从 0 放大到 Inspector 中设置的正常 scale（paperBaseScale）
            fuzhiPaper.transform.localScale = Vector3.zero;
            StartCoroutine(PlayFuzhiAppear());
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
        // ???????
        Debug.Log("?????");
        canDraw = false;
        if (fuzhiPaper != null)
        {
            fuzhiPaper.SetActive(false);
        }
        ClearCanvas();
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

        // 记录符纸初始状态（用于抖动与淡出）
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

        // 记录线条初始发光强度与原始点位
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

        // 线条逐渐增强发光 + 符纸与线一起抖动
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

                // 发光增强
                if (lr.material != null && lr.material.HasProperty("_GlowIntensity"))
                {
                    lr.material.SetFloat("_GlowIntensity", Mathf.Lerp(initialGlow, lineGlowTarget, ease));
                }

                // 抖动位移
                Vector2 offset2D = Random.insideUnitCircle * shakeAmplitude * ease;
                Vector3 offset3D = new Vector3(offset2D.x, offset2D.y, 0f);
                for (int i = 0; i < orig.Length; i++)
                {
                    lr.SetPosition(i, orig[i] + offset3D);
                }
            }

            if (fuzhiPaper != null)
            {
                Vector2 offset2D = Random.insideUnitCircle * shakeAmplitude * ease;
                fuzhiPaper.transform.localPosition =
                    paperStartLocalPos + new Vector3(offset2D.x, offset2D.y, 0f);
            }

            yield return null;
        }

        // 符纸与线一起炸开：符纸放大并淡出，线条从中心向外扩散
        if (fuzhiPaper != null)
        {
            float et = 0f;
            while (et < explodeDuration)
            {
                et += Time.deltaTime;
                float lerp = Mathf.Clamp01(et / explodeDuration);
                float ease = 1f - (1f - lerp) * (1f - lerp);

                // 最终大小 = Inspector 中设置的正常大小 * explodeScaleMultiplier
                float scale = Mathf.Lerp(1f, explodeScaleMultiplier, ease);
                fuzhiPaper.transform.localScale = paperStartScale * scale;

                // 线条围绕符纸中心同步放大
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

            // 还原符纸的本地变换，为下一次召唤做准备
            fuzhiPaper.transform.localPosition = paperStartLocalPos;
            fuzhiPaper.transform.localScale = paperStartScale;
        }

        ConfirmSummon();
    }

    private IEnumerator PlayFuzhiAppear()
    {
        if (fuzhiPaper == null) yield break;

        Vector3 startScale = Vector3.zero;
        // 出现后的目标大小 = Inspector 中设置的正常缩放
        Vector3 endScale = paperBaseScale;
        float t = 0f;

        while (t < appearDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / appearDuration);
            // 先快后慢的缓动
            lerp = 1f - (1f - lerp) * (1f - lerp);
            fuzhiPaper.transform.localScale = Vector3.Lerp(startScale, endScale, lerp);
            yield return null;
        }

        fuzhiPaper.transform.localScale = endScale;
    }
}