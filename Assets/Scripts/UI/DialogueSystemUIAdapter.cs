using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DialogueSystemUIAdapter : MonoBehaviour
{
    [Header("Panel Behavior")]
    [SerializeField] private bool closeActivePanelsOnConversationStart = true;
    [SerializeField] private bool blockPanelOpeningDuringConversation = true;

    [Header("Optional UI Roots")]
    [SerializeField] private GameObject[] uiRootsToHideDuringConversation;
    [SerializeField] private bool restoreHiddenRootsOnConversationEnd = true;

    private readonly Dictionary<GameObject, bool> hiddenRootStates = new Dictionary<GameObject, bool>();
    private readonly HashSet<GameObject> runtimeRootsToHideDuringConversation = new HashSet<GameObject>();
    private bool isSubscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!isSubscribed)
            TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        RestoreHiddenRoots();

        if (blockPanelOpeningDuringConversation)
            UIPanelCoordinator.SetDisplayLocked(false);
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
            return;

        if (DialogueManager.instance == null)
            return;

        DialogueManager.instance.conversationStarted += OnConversationStarted;
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
            return;

        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationStarted -= OnConversationStarted;
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        }

        isSubscribed = false;
    }

    private void OnConversationStarted(Transform actor)
    {
        if (blockPanelOpeningDuringConversation)
            UIPanelCoordinator.SetDisplayLocked(true, closeActivePanelsOnConversationStart);

        if (closeActivePanelsOnConversationStart && !blockPanelOpeningDuringConversation)
            UIPanelCoordinator.HideAllActivePanels();

        HideConfiguredRoots();
    }

    private void OnConversationEnded(Transform actor)
    {
        if (blockPanelOpeningDuringConversation)
            UIPanelCoordinator.SetDisplayLocked(false);

        if (restoreHiddenRootsOnConversationEnd)
            RestoreHiddenRoots();
    }

    private void HideConfiguredRoots()
    {
        hiddenRootStates.Clear();

        if (uiRootsToHideDuringConversation != null)
        {
            for (int i = 0; i < uiRootsToHideDuringConversation.Length; i++)
                TryHideRoot(uiRootsToHideDuringConversation[i]);
        }

        if (runtimeRootsToHideDuringConversation.Count == 0)
            return;

        var snapshot = new List<GameObject>(runtimeRootsToHideDuringConversation);
        for (int i = 0; i < snapshot.Count; i++)
            TryHideRoot(snapshot[i]);
    }

    private void RestoreHiddenRoots()
    {
        if (hiddenRootStates.Count == 0)
            return;

        foreach (KeyValuePair<GameObject, bool> pair in hiddenRootStates)
        {
            if (pair.Key == null)
                continue;

            pair.Key.SetActive(pair.Value);
        }

        hiddenRootStates.Clear();
    }

    public void RegisterRuntimeRootToHide(GameObject root)
    {
        if (root == null)
            return;

        runtimeRootsToHideDuringConversation.Add(root);
    }

    public void UnregisterRuntimeRootToHide(GameObject root)
    {
        if (root == null)
            return;

        runtimeRootsToHideDuringConversation.Remove(root);
        hiddenRootStates.Remove(root);
    }

    private void TryHideRoot(GameObject root)
    {
        if (root == null)
            return;

        bool wasActive = root.activeSelf;
        hiddenRootStates[root] = wasActive;
        if (wasActive)
            root.SetActive(false);
    }
}
