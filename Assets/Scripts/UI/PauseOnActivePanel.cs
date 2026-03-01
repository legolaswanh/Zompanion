using UnityEngine;

[DisallowMultipleComponent]
public class PauseOnActivePanel : MonoBehaviour
{
    [SerializeField] private bool pauseWhileOpen = true;

    private bool _pauseRequested;

    private void OnEnable()
    {
        if (!pauseWhileOpen)
            return;

        RequestPause();
    }

    private void OnDisable()
    {
        ReleasePause();
    }

    private void OnDestroy()
    {
        ReleasePause();
    }

    public void SetPauseWhileOpen(bool enabled)
    {
        pauseWhileOpen = enabled;

        if (!pauseWhileOpen)
        {
            ReleasePause();
            return;
        }

        if (isActiveAndEnabled)
            RequestPause();
    }

    private void RequestPause()
    {
        if (_pauseRequested)
            return;

        UIPauseState.RequestPause(this);
        _pauseRequested = true;
    }

    private void ReleasePause()
    {
        if (!_pauseRequested)
            return;

        UIPauseState.ReleasePause(this);
        _pauseRequested = false;
    }
}
