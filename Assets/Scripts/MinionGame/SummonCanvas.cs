using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Start()
    {
        // ???????????????????
        if (fuzhiPaper != null && Camera.main != null)
        {
            var cam = Camera.main;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane + 1f);
            Vector3 worldPos = cam.ScreenToWorldPoint(screenCenter);
            fuzhiPaper.transform.position = worldPos;
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
            fuzhiPaper.SetActive(true);
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
        yield return new WaitForSeconds(autoFinishDelay);
        ConfirmSummon();
    }
}