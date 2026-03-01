using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIPanelCoordinator
{
    private const string BlockerObjectName = "__UIPanelBlocker";
    private static readonly HashSet<GameObject> Panels = new HashSet<GameObject>();
    private static readonly Dictionary<Transform, Image> BlockersByParent = new Dictionary<Transform, Image>();

    public static void Register(GameObject panel)
    {
        if (panel == null)
            return;

        CleanupDeadReferences();
        Panels.Add(panel);
        EnsureStateNotifier(panel);
        RefreshBlockers();
    }

    public static void Unregister(GameObject panel)
    {
        if (panel == null)
            return;

        Panels.Remove(panel);
        RefreshBlockers();
    }

    public static void ToggleExclusive(GameObject panel)
    {
        if (panel == null)
            return;

        if (panel.activeSelf)
            Hide(panel);
        else
            ShowExclusive(panel);
    }

    public static void ShowExclusive(GameObject panel)
    {
        if (panel == null)
            return;

        Register(panel);

        var snapshot = new List<GameObject>(Panels);
        for (int i = 0; i < snapshot.Count; i++)
        {
            GameObject other = snapshot[i];
            if (other == null || other == panel)
                continue;

            if (other.activeSelf)
                other.SetActive(false);
        }

        if (!panel.activeSelf)
            panel.SetActive(true);

        panel.transform.SetAsLastSibling();
        RefreshBlockers(panel);
    }

    public static void Hide(GameObject panel)
    {
        if (panel == null)
            return;

        if (panel.activeSelf)
            panel.SetActive(false);

        RefreshBlockers();
    }

    public static void NotifyPanelStateChanged(GameObject panel)
    {
        if (panel == null)
            return;

        if (Panels.Contains(panel))
            RefreshBlockers(panel.activeInHierarchy ? panel : null);
    }

    private static void CleanupDeadReferences()
    {
        var deadPanels = new List<GameObject>();
        foreach (GameObject panel in Panels)
        {
            if (panel == null)
                deadPanels.Add(panel);
        }

        for (int i = 0; i < deadPanels.Count; i++)
            Panels.Remove(deadPanels[i]);

        var deadParents = new List<Transform>();
        foreach (var pair in BlockersByParent)
        {
            if (pair.Key == null || pair.Value == null)
                deadParents.Add(pair.Key);
        }

        for (int i = 0; i < deadParents.Count; i++)
            BlockersByParent.Remove(deadParents[i]);
    }

    private static void RefreshBlockers(GameObject preferredTopPanel = null)
    {
        CleanupDeadReferences();

        var topPanelByParent = new Dictionary<Transform, GameObject>();
        foreach (GameObject panel in Panels)
        {
            if (panel == null || !panel.activeInHierarchy)
                continue;

            Transform parent = panel.transform.parent;
            if (parent == null)
                continue;

            if (!topPanelByParent.TryGetValue(parent, out GameObject currentTop) || currentTop == null)
            {
                topPanelByParent[parent] = panel;
                continue;
            }

            if (panel.transform.GetSiblingIndex() >= currentTop.transform.GetSiblingIndex())
                topPanelByParent[parent] = panel;
        }

        if (preferredTopPanel != null && preferredTopPanel.activeInHierarchy)
        {
            Transform parent = preferredTopPanel.transform.parent;
            if (parent != null)
                topPanelByParent[parent] = preferredTopPanel;
        }

        var parentSnapshot = new List<Transform>(BlockersByParent.Keys);
        for (int i = 0; i < parentSnapshot.Count; i++)
        {
            Transform parent = parentSnapshot[i];
            if (!topPanelByParent.ContainsKey(parent) && BlockersByParent.TryGetValue(parent, out Image blocker) && blocker != null)
                blocker.gameObject.SetActive(false);
        }

        foreach (var pair in topPanelByParent)
        {
            Transform parent = pair.Key;
            GameObject topPanel = pair.Value;
            if (parent == null || topPanel == null)
                continue;

            Image blocker = EnsureBlocker(parent);
            if (blocker == null)
                continue;

            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling();
            topPanel.transform.SetAsLastSibling();
        }
    }

    private static Image EnsureBlocker(Transform parent)
    {
        if (parent == null)
            return null;

        if (BlockersByParent.TryGetValue(parent, out Image existing) && existing != null)
            return existing;

        RectTransform parentRect = parent as RectTransform;
        if (parentRect == null)
            return null;

        var blockerObj = new GameObject(BlockerObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        blockerObj.transform.SetParent(parent, false);

        RectTransform rect = blockerObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = blockerObj.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.35f);
        image.raycastTarget = true;

        LayoutElement layout = blockerObj.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;

        blockerObj.SetActive(false);
        BlockersByParent[parent] = image;
        return image;
    }

    private static void EnsureStateNotifier(GameObject panel)
    {
        if (panel == null)
            return;

        if (panel.GetComponent<UIPanelStateNotifier>() == null)
            panel.AddComponent<UIPanelStateNotifier>();
    }
}
