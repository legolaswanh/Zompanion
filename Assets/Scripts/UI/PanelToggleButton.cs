using UnityEngine;

public class PanelToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private bool pauseGameWhenPanelOpen = true;

    private void Awake()
    {
        EnsurePanelRegistration();
        EnsurePauseBinding();
    }

    public void Toggle()
    {
        if (targetPanel == null) return;
        UIPanelCoordinator.ToggleExclusive(targetPanel);
    }

    public void Show()
    {
        if (targetPanel == null) return;
        UIPanelCoordinator.ShowExclusive(targetPanel);
    }

    public void Hide()
    {
        if (targetPanel == null) return;
        UIPanelCoordinator.Hide(targetPanel);
    }

    private void EnsurePanelRegistration()
    {
        if (targetPanel == null)
            return;

        UIPanelCoordinator.Register(targetPanel);
    }

    private void EnsurePauseBinding()
    {
        if (!pauseGameWhenPanelOpen || targetPanel == null)
            return;

        PauseOnActivePanel pauseHandler = targetPanel.GetComponent<PauseOnActivePanel>();
        if (pauseHandler == null)
            pauseHandler = targetPanel.AddComponent<PauseOnActivePanel>();

        pauseHandler.SetPauseWhileOpen(true);
    }
}
