using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zompanion.ZombieSystem;

public class ZombieAssignmentController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject assignmentRoot;

    [Header("Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text buttonText;

    [Header("Monk Effect")]
    [SerializeField] private GameObject monkEffectRoot;
    [SerializeField] private GameObject unfollowText;
    [SerializeField] private GameObject followText;

    [Header("Official Effect")]
    [SerializeField] private GameObject officialEffectRoot;
    [SerializeField] private GameObject giftText;
    [SerializeField] private GameObject travelText;

    private ZombieManager _zombieManager;
    private string _boundDefinitionId;

    private void OnEnable()
    {
        if (_zombieManager != null)
        {
            _zombieManager.OnZombieListChanged += OnZombieListChanged;
            _zombieManager.OnCodexChanged += OnCodexChanged;
        }
    }

    private void OnDisable()
    {
        if (_zombieManager != null)
        {
            _zombieManager.OnZombieListChanged -= OnZombieListChanged;
            _zombieManager.OnCodexChanged -= OnCodexChanged;
        }
    }

    /// <summary>
    /// 绑定选中僵尸，刷新 Assignment 显示。definitionId 为空或 zombieManager 为空时隐藏。
    /// </summary>
    public void Bind(string definitionId, ZombieManager zombieManager)
    {
        _zombieManager = zombieManager;
        _boundDefinitionId = definitionId;

        if (_zombieManager != null)
        {
            _zombieManager.OnZombieListChanged -= OnZombieListChanged;
            _zombieManager.OnCodexChanged -= OnCodexChanged;
            _zombieManager.OnZombieListChanged += OnZombieListChanged;
            _zombieManager.OnCodexChanged += OnCodexChanged;
        }

        if (assignmentRoot == null)
            return;

        if (string.IsNullOrWhiteSpace(definitionId) || zombieManager == null)
        {
            assignmentRoot.SetActive(false);
            return;
        }

        ZombieDefinitionSO definition = zombieManager.GetDefinition(definitionId);
        if (definition == null)
        {
            assignmentRoot.SetActive(false);
            return;
        }

        bool unlocked = zombieManager.IsZombieCodexUnlocked(definition.DefinitionId);
        if (!unlocked)
        {
            assignmentRoot.SetActive(false);
            return;
        }

        assignmentRoot.SetActive(true);

        if (actionButton != null)
            actionButton.onClick.RemoveAllListeners();
        if (buttonText != null)
            buttonText.raycastTarget = false;

        switch (definition.Category)
        {
            case ZombieCategory.Monk:
                if (monkEffectRoot != null) monkEffectRoot.SetActive(true);
                if (officialEffectRoot != null) officialEffectRoot.SetActive(false);
                if (actionButton != null)
                    actionButton.onClick.AddListener(OnMonkFollowClick);
                RefreshMonkTexts();
                break;

            case ZombieCategory.Officer:
                if (monkEffectRoot != null) monkEffectRoot.SetActive(false);
                if (officialEffectRoot != null) officialEffectRoot.SetActive(true);
                if (actionButton != null)
                    actionButton.onClick.AddListener(OnOfficerExploreClick);
                RefreshOfficialTexts();
                break;

            default:
                assignmentRoot.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// 隐藏 Assignment 区域。
    /// </summary>
    public void Hide()
    {
        _boundDefinitionId = null;
        if (assignmentRoot != null)
            assignmentRoot.SetActive(false);
    }

    private void OnMonkFollowClick()
    {
        if (string.IsNullOrWhiteSpace(_boundDefinitionId) || _zombieManager == null)
            return;
        _zombieManager.TryToggleFollowByDefinitionId(_boundDefinitionId);
        RefreshMonkTexts();
    }

    private void OnOfficerExploreClick()
    {
        if (string.IsNullOrWhiteSpace(_boundDefinitionId) || _zombieManager == null)
            return;
        bool traveling = _zombieManager.IsDefinitionWorking(_boundDefinitionId);
        if (traveling)
        {
            // TODO: 实现取消探索功能
            return;
        }
        // TODO: 实现外出探索功能
    }

    private void RefreshMonkTexts()
    {
        if (string.IsNullOrWhiteSpace(_boundDefinitionId) || _zombieManager == null)
            return;
        bool following = _zombieManager.IsDefinitionFollowing(_boundDefinitionId);
        bool storiesUnlocked = _zombieManager.AreAllStoriesUnlockedForZombie(_boundDefinitionId);
        if (buttonText != null)
            buttonText.text = following ? "Unfollow" : (storiesUnlocked ? "Follow" : "Unlock all stories to function");
        if (actionButton != null)
            actionButton.interactable = following || storiesUnlocked;
        if (unfollowText != null) unfollowText.SetActive(!following);
        if (followText != null) followText.SetActive(following);
    }

    private void RefreshOfficialTexts()
    {
        if (string.IsNullOrWhiteSpace(_boundDefinitionId) || _zombieManager == null)
            return;
        bool storiesUnlocked = _zombieManager.AreAllStoriesUnlockedForZombie(_boundDefinitionId);
        bool traveling = _zombieManager.IsDefinitionWorking(_boundDefinitionId);
        if (buttonText != null)
            buttonText.text = traveling ? "Stop Explore" : (storiesUnlocked ? "Explore" : "Unlock all stories to function");
        if (actionButton != null)
            actionButton.interactable = traveling || storiesUnlocked;
        if (giftText != null) giftText.SetActive(!traveling);
        if (travelText != null) travelText.SetActive(traveling);
    }

    private void OnZombieListChanged()
    {
        RefreshCurrentState();
    }

    private void OnCodexChanged()
    {
        RefreshCurrentState();
    }

    private void RefreshCurrentState()
    {
        if (string.IsNullOrWhiteSpace(_boundDefinitionId) || _zombieManager == null)
            return;
        ZombieDefinitionSO definition = _zombieManager.GetDefinition(_boundDefinitionId);
        if (definition == null)
            return;
        if (definition.Category == ZombieCategory.Monk)
            RefreshMonkTexts();
        else if (definition.Category == ZombieCategory.Officer)
            RefreshOfficialTexts();
    }
}
