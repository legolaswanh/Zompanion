using UnityEngine;

public class UIPanelEscapeHotkey : MonoBehaviour
{
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private static UIPanelEscapeHotkey _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        var go = new GameObject("__UIPanelEscapeHotkey");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UIPanelEscapeHotkey>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(closeKey))
            return;

        // 游戏内由 PauseMenuController 处理 ESC，避免重复或冲突
        if (PauseMenuController.EscapeConsumedByPauseMenu)
        {
            PauseMenuController.EscapeConsumedByPauseMenu = false;
            return;
        }

        UIPanelCoordinator.HideTopmostActivePanel();
    }
}
