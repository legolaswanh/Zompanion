using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // 1. 引用 UI 预制体
    [Header("UI Prefabs")]
    [SerializeField] private GameObject mainHudPrefab;

    [Header("背包显示场景")]
    [Tooltip("背包 UI 只在这些场景中显示；默认进入首个玩法场景 HomeScene 后才激活")]
    [SerializeField] private string[] gameSceneNames = { "HomeScene", "TestScene", "ExplorationScene" };

    private GameObject currentHudInstance;

    private void Awake()
    {
        // 2. 核心：确保这个物体（以及挂载的Manager）在切换场景时不会被销毁
        // 注意：这行代码会让挂载此脚本的物体（GameManager）变成“不死之身”
        DontDestroyOnLoad(gameObject);
        
        // 3. 检查单例（防止切回主菜单再切回来时出现两个 Manager）
        // 简单处理：如果发现已经有别的 UIManager，就把自己销毁
        if (FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isGameScene = IsGameScene(scene.name);
        if (isGameScene)
        {
            EnsureHudExists();
            if (currentHudInstance != null)
                currentHudInstance.SetActive(true);
        }
        else if (currentHudInstance != null)
        {
            currentHudInstance.SetActive(false);
        }
    }

    private bool IsGameScene(string sceneName)
    {
        if (gameSceneNames == null) return false;
        foreach (var name in gameSceneNames)
        {
            if (name == sceneName) return true;
        }
        return false;
    }

    private void EnsureHudExists()
    {
        if (currentHudInstance == null && mainHudPrefab != null)
        {
            currentHudInstance = Instantiate(mainHudPrefab);
            
            // 关键：把生成的 UI 也设为“不销毁”，或者让它成为 Manager 的子物体
            DontDestroyOnLoad(currentHudInstance);
        }
    }
}