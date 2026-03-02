using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelStateNotifier : MonoBehaviour
{
    private void OnEnable()
    {
        UIPanelCoordinator.NotifyPanelStateChanged(gameObject);
    }

    private void OnDisable()
    {
        UIPanelCoordinator.NotifyPanelStateChanged(gameObject);
    }

    private void OnDestroy()
    {
        UIPanelCoordinator.Unregister(gameObject);
    }
}
