using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Code.Scripts;

/// <summary>
/// 主界面控制器：处理 Start、Load、Save、Exit 按钮逻辑。
/// 与 GameManager、SaveSystem、TransitionSceneData 整合。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("按钮引用（可留空，将自动查找）")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button exitButton;

    void Start()
    {
        ResolveButtons();
        BindButtons();
        RefreshLoadSaveState();
    }

    void ResolveButtons()
    {
        if (startButton == null) startButton = FindButton("Start");
        if (loadButton == null) loadButton = FindButton("Load");
        if (saveButton == null) saveButton = FindButton("Save");
        if (exitButton == null) exitButton = FindButton("Exit");
    }

    Button FindButton(string name)
    {
        var buttons = FindObjectsOfType<Button>(true);
        foreach (var b in buttons)
        {
            if (b.gameObject.name == name) return b;
        }
        return null;
    }

    void BindButtons()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartGame);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadGame);
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveGame);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitGame);
    }

    void RefreshLoadSaveState()
    {
        if (loadButton != null)
            loadButton.interactable = SaveSystem.HasSaveData();

        // 主界面时无可保存的进度，Save 按钮禁用；无 Save 按钮时忽略
        if (saveButton != null)
            saveButton.interactable = IsInGameScene();
    }

    bool IsInGameScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.name != "MainMenu" && scene.name != "Persistent";
    }

    void OnStartGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] GameManager 不存在");
            return;
        }
        GameManager.Instance.LoadNewGame();
    }

    void OnLoadGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] GameManager 不存在");
            return;
        }
        if (!SaveSystem.HasSaveData())
        {
            Debug.LogWarning("[MainMenuController] 无存档");
            return;
        }
        GameManager.Instance.LoadSavedGame();
    }

    void OnSaveGame()
    {
        if (!IsInGameScene())
        {
            Debug.LogWarning("[MainMenuController] 主界面无可保存进度");
            return;
        }
        SaveSystem.Save();
        RefreshLoadSaveState();
    }

    void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 供游戏中暂停菜单等调用：保存并可选返回主菜单。
    /// </summary>
    public void SaveAndReturnToMainMenu()
    {
        if (IsInGameScene())
            SaveSystem.Save();
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }
}
