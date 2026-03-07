using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Code.Scripts;

/// <summary>
/// 游戏内 ESC 暂停菜单控制器：按 ESC 显示/隐藏 PauseCanvas，暂停游戏，提供保存/读取/退出功能。
/// 预制体可通过 Inspector 指定，或放入 Resources/UI/PauseCanvas 自动加载。
/// </summary>
[DefaultExecutionOrder(-100)]
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    /// <summary>
    /// 当暂停菜单处理了 ESC 时设置，供 UIPanelEscapeHotkey 跳过同一帧的处理。
    /// </summary>
    public static bool EscapeConsumedByPauseMenu { get; set; }

    [Header("配置（可留空，将尝试从 Resources/UI/PauseCanvas 加载）")]
    [SerializeField] private GameObject pauseCanvasPrefab;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject("PauseMenuController");
        DontDestroyOnLoad(go);
        go.AddComponent<PauseMenuController>();
    }

    [Header("按钮（可留空，将自动查找）")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button exitButton;

    private GameObject _pauseCanvasInstance;
    private GameObject _panelRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsurePauseCanvasExists();
        EnsurePauseOnActivePanel();
        BindButtons();
        RefreshButtonStates();

        // 初始状态：隐藏
        HidePauseMenu();
    }

    private void Update()
    {
        if (!IsInGameScene())
            return;
        if (!Input.GetKeyDown(toggleKey))
            return;

        EscapeConsumedByPauseMenu = true;

        if (IsPauseMenuVisible())
        {
            HidePauseMenu();
            return;
        }

        // 优先关闭其他已打开的 UI 面板（如背包、图鉴）
        if (UIPanelCoordinator.HideTopmostActivePanel())
            return;

        ShowPauseMenu();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsurePauseCanvasExists()
    {
        if (_pauseCanvasInstance != null)
            return;

        if (pauseCanvasPrefab == null)
        {
            pauseCanvasPrefab = Resources.Load<GameObject>("UI/PauseCanvas");
            if (pauseCanvasPrefab == null)
            {
                Debug.LogError("[PauseMenuController] 未找到 PauseCanvas 预制体，请在 Inspector 中指定或放入 Resources/UI/PauseCanvas");
                return;
            }
        }

        _pauseCanvasInstance = Instantiate(pauseCanvasPrefab);
        _pauseCanvasInstance.name = "PauseCanvas";
        DontDestroyOnLoad(_pauseCanvasInstance);

        // 查找 Panel 子物体作为面板根（用于 UIPanelCoordinator 和 PauseOnActivePanel）
        Transform panel = _pauseCanvasInstance.transform.Find("Panel");
        _panelRoot = panel != null ? panel.gameObject : _pauseCanvasInstance;
    }

    private void EnsurePauseOnActivePanel()
    {
        if (_panelRoot == null)
            return;

        if (_panelRoot.GetComponent<PauseOnActivePanel>() == null)
            _panelRoot.AddComponent<PauseOnActivePanel>();
    }

    private void BindButtons()
    {
        ResolveButtons();
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveGame);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadGame);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitGame);
    }

    private void ResolveButtons()
    {
        if (_pauseCanvasInstance == null)
            return;

        if (saveButton == null) saveButton = FindButton("Save");
        if (loadButton == null) loadButton = FindButton("Load");
        if (exitButton == null) exitButton = FindButton("Exit");
    }

    private Button FindButton(string name)
    {
        if (_pauseCanvasInstance == null)
            return null;

        Button[] buttons = _pauseCanvasInstance.GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            if (b.gameObject.name == name)
                return b;
        }
        return null;
    }

    private void RefreshButtonStates()
    {
        if (loadButton != null)
            loadButton.interactable = SaveSystem.HasSaveData();
        if (saveButton != null)
            saveButton.interactable = IsInGameScene();
    }

    private void ShowPauseMenu()
    {
        if (_pauseCanvasInstance == null)
            return;

        RefreshButtonStates();
        _pauseCanvasInstance.SetActive(true);
        UIPanelCoordinator.ShowExclusive(_panelRoot);
    }

    private void HidePauseMenu()
    {
        if (_pauseCanvasInstance == null)
            return;

        UIPanelCoordinator.Hide(_panelRoot);
        _pauseCanvasInstance.SetActive(false);
    }

    private bool IsPauseMenuVisible()
    {
        return _pauseCanvasInstance != null && _pauseCanvasInstance.activeInHierarchy;
    }

    private static bool IsInGameScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.name != "MainMenu" && scene.name != "Persistent";
    }

    private void OnSaveGame()
    {
        if (!IsInGameScene())
        {
            Debug.LogWarning("[PauseMenuController] 主界面无可保存进度");
            return;
        }
        SaveSystem.Save();
        RefreshButtonStates();
        HidePauseMenu();
    }

    private void OnLoadGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PauseMenuController] GameManager 不存在");
            return;
        }
        if (!SaveSystem.HasSaveData())
        {
            Debug.LogWarning("[PauseMenuController] 无存档");
            return;
        }
        HidePauseMenu();
        GameManager.Instance.LoadSavedGame();
    }

    private void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
