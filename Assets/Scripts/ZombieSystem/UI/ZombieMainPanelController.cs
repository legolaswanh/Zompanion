using UnityEngine;
using UnityEngine.UI;

public class ZombieMainPanelController : MonoBehaviour
{
    public static ZombieMainPanelController Instance { get; private set; }

    public enum ZombiePage
    {
        Codex = 0,
        Control = 1
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject codexPage;
    [SerializeField] private ZombieCodexPanelController codexPanelController;
    [SerializeField] private GameObject controlPage;
    [SerializeField] private bool pauseGameWhenMainPanelOpen = true;

    [Header("Buttons")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button bookmarkCodexButton;
    [SerializeField] private Button bookmarkControlButton;

    [Header("Bookmark Motion (Optional)")]
    [SerializeField] private ZombieBookmarkTabAnimator bookmarkCodexMotion;
    [SerializeField] private ZombieBookmarkTabAnimator bookmarkControlMotion;

    [Header("Visual (Optional)")]
    [SerializeField] private GameObject bookmarkCodexSelected;
    [SerializeField] private GameObject bookmarkControlSelected;

    [SerializeField] private ZombiePage defaultPage = ZombiePage.Codex;

    private ZombiePage _currentPage;

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
        _currentPage = defaultPage;
        ApplyPage(_currentPage, instant: true);
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

        OpenToPage(ZombiePage.Codex);
        if (codexPanelController != null)
            codexPanelController.SelectAndShowNewZombie(definitionId);
    }

    public void OpenToCodex()
    {
        OpenToPage(ZombiePage.Codex);
    }

    public void OpenToControl()
    {
        OpenToPage(ZombiePage.Control);
    }

    public void OpenToPage(ZombiePage page)
    {
        _currentPage = page;
        ApplyPage(_currentPage, instant: false);
        if (mainPanel != null)
            UIPanelCoordinator.ShowExclusive(mainPanel);
    }

    public void SwitchToCodex()
    {
        SwitchToPage(ZombiePage.Codex);
    }

    public void SwitchToControl()
    {
        SwitchToPage(ZombiePage.Control);
    }

    public void SwitchToPage(ZombiePage page)
    {
        _currentPage = page;
        ApplyPage(_currentPage, instant: false);
    }

    public void ClosePanel()
    {
        if (mainPanel != null)
            UIPanelCoordinator.Hide(mainPanel);
    }

    private void BindButtons(bool bind)
    {
        BindButton(exitButton, ClosePanel, bind);
        BindButton(bookmarkCodexButton, SwitchToCodex, bind);
        BindButton(bookmarkControlButton, SwitchToControl, bind);
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

    private void ApplyPage(ZombiePage page, bool instant)
    {
        bool codexActive = page == ZombiePage.Codex;
        if (codexPage != null)
            codexPage.SetActive(codexActive);
        if (controlPage != null)
            controlPage.SetActive(!codexActive);

        if (bookmarkCodexMotion != null)
            bookmarkCodexMotion.SetSelected(codexActive, instant);
        if (bookmarkControlMotion != null)
            bookmarkControlMotion.SetSelected(!codexActive, instant);

        if (bookmarkCodexSelected != null)
            bookmarkCodexSelected.SetActive(codexActive);
        if (bookmarkControlSelected != null)
            bookmarkControlSelected.SetActive(!codexActive);

        if (bookmarkCodexButton != null)
            bookmarkCodexButton.interactable = true;
        if (bookmarkControlButton != null)
            bookmarkControlButton.interactable = true;
    }
}
