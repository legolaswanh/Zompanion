using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZombieMainPanelController : MonoBehaviour
{
    public static ZombieMainPanelController Instance { get; private set; }

    /// <summary>
    /// 获取可用的控制器。若 Instance 为空则尝试在场景中查找；若仍找不到则请求 UIManager 创建 HUD 后再查一次。
    /// </summary>
    public static ZombieMainPanelController GetOrFind()
    {
        if (Instance != null)
            return Instance;
        var found = FindObjectOfType<ZombieMainPanelController>(true);
        if (found != null)
        {
            Instance = found;
            return found;
        }
        UIManager.EnsureMainHudExists();
        found = FindObjectOfType<ZombieMainPanelController>(true);
        if (found != null)
        {
            Instance = found;
            return found;
        }
        return null;
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject codexPage;
    [SerializeField] private ZombieCodexPanelController codexPanelController;
    [SerializeField] private bool pauseGameWhenMainPanelOpen = true;

    [Header("Buttons")]
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this;

        if (mainPanel == null)
            mainPanel = gameObject;

        if (codexPanelController == null && codexPage != null)
            codexPanelController = codexPage.GetComponent<ZombieCodexPanelController>();

        if (mainPanel != null)
        {
            UIPanelCoordinator.Register(mainPanel);
            EnsurePauseBinding();
        }

        BindButtons(true);
        if (codexPage != null)
            codexPage.SetActive(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        BindButtons(false);
    }

    /// <summary>
    /// 打开 Zombie 面板并聚焦到刚组装的新僵尸，播放头像解锁动画。
    /// 右侧显示头像、名字（已有）和背景故事。
    /// </summary>
    public void OpenForNewZombie(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return;

        if (codexPanelController == null && codexPage != null)
            codexPanelController = codexPage.GetComponent<ZombieCodexPanelController>();

        OpenToCodex();
        if (codexPanelController != null)
        {
            StartCoroutine(SelectNewZombieNextFrame(definitionId, goToStoryPage: false));
        }
    }

    /// <summary>
    /// 打开僵尸面板并显示指定僵尸的剧情页（用于提交道具解锁 story 后）。
    /// </summary>
    public void OpenForNewStory(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return;

        OpenToCodex();
        if (codexPanelController != null)
        {
            StartCoroutine(SelectNewZombieNextFrame(definitionId, goToStoryPage: true));
        }
    }

    private IEnumerator SelectNewZombieNextFrame(string definitionId, bool goToStoryPage)
    {
        yield return null;
        if (codexPanelController != null)
        {
            codexPanelController.SelectAndShowNewZombie(definitionId, goToStoryPage);
        }
    }

    public void OpenToCodex()
    {
        if (mainPanel != null)
        {
            EnsurePanelCanShow();
            UIPanelCoordinator.ShowExclusive(mainPanel);
        }
        if (codexPage != null)
            codexPage.SetActive(true);
    }

    /// <summary>
    /// 确保面板能显示：解除显示锁定，激活父级 Canvas（对话期间可能被隐藏）。
    /// </summary>
    private void EnsurePanelCanShow()
    {
        UIPanelCoordinator.SetDisplayLocked(false);
        Transform root = mainPanel.transform.root;
        if (root != null && root.gameObject != mainPanel && !root.gameObject.activeSelf)
            root.gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        if (mainPanel != null)
            UIPanelCoordinator.Hide(mainPanel);
    }

    private void BindButtons(bool bind)
    {
        BindButton(exitButton, ClosePanel, bind);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action, bool bind)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        if (bind)
            button.onClick.AddListener(action);
    }

    private void EnsurePauseBinding()
    {
        if (!pauseGameWhenMainPanelOpen || mainPanel == null)
            return;

        PauseOnActivePanel pauseHandler = mainPanel.GetComponent<PauseOnActivePanel>();
        if (pauseHandler == null)
            pauseHandler = mainPanel.AddComponent<PauseOnActivePanel>();

        pauseHandler.SetPauseWhileOpen(true);
    }
}
