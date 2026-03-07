using System.Linq;
using Code.Scripts;
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
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text giftMessageText;
    [SerializeField] private Button itemImageButton;
    [SerializeField] private GameObject itemDisplayCanvasPrefab;

    private ZombieManager _zombieManager;
    private string _boundDefinitionId;

    private bool _subscribedToExplore;

    private void OnEnable()
    {
        if (_zombieManager != null)
        {
            _zombieManager.OnZombieListChanged += OnZombieListChanged;
            _zombieManager.OnCodexChanged += OnCodexChanged;
        }

        TrySubscribeToExploreService();

        if (itemImageButton != null)
            itemImageButton.onClick.AddListener(OnGiftItemClicked);
    }

    private void TrySubscribeToExploreService()
    {
        if (_subscribedToExplore || ZombieExploreService.Instance == null)
            return;
        ZombieExploreService.Instance.OnGiftArrived += OnExploreGiftArrived;
        ZombieExploreService.Instance.OnGiftClaimed += OnExploreGiftClaimed;
        _subscribedToExplore = true;
    }

    private void OnDisable()
    {
        if (_zombieManager != null)
        {
            _zombieManager.OnZombieListChanged -= OnZombieListChanged;
            _zombieManager.OnCodexChanged -= OnCodexChanged;
        }

        if (_subscribedToExplore && ZombieExploreService.Instance != null)
        {
            ZombieExploreService.Instance.OnGiftArrived -= OnExploreGiftArrived;
            ZombieExploreService.Instance.OnGiftClaimed -= OnExploreGiftClaimed;
            _subscribedToExplore = false;
        }

        if (itemImageButton != null)
            itemImageButton.onClick.RemoveListener(OnGiftItemClicked);
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

        TrySubscribeToExploreService();

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

        var exploreService = ZombieExploreService.Instance;
        if (exploreService != null && exploreService.HasPendingGift && exploreService.PendingDefinitionId == _boundDefinitionId)
        {
            OnGiftItemClicked();
            return;
        }

        var zombie = _zombieManager.Zombies.FirstOrDefault(z => z.definitionId == _boundDefinitionId);
        if (zombie == null)
            return;

        bool traveling = _zombieManager.IsDefinitionWorking(_boundDefinitionId);

        if (traveling)
        {
            _zombieManager.SetWorkState(zombie.instanceId, false);
            exploreService?.StopExplore(_boundDefinitionId);
            RefreshOfficialTexts();
            return;
        }

        if (!_zombieManager.AreAllStoriesUnlockedForZombie(_boundDefinitionId))
            return;

        if (exploreService == null)
            return;

        _zombieManager.CaptureZombieCurrentTransformAsHomeAnchor(zombie.instanceId);
        _zombieManager.SetWorkState(zombie.instanceId, true);
        if (exploreService.StartExplore(_boundDefinitionId))
            RefreshOfficialTexts();
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
        var exploreService = ZombieExploreService.Instance;
        bool hasPendingForThis = exploreService != null && exploreService.HasPendingGift &&
                                 exploreService.PendingDefinitionId == _boundDefinitionId;

        if (buttonText != null)
            buttonText.text = traveling ? "Stop Explore" : (storiesUnlocked ? "Explore" : "Unlock all stories to function");
        if (actionButton != null)
            actionButton.interactable = traveling || storiesUnlocked;

        if (hasPendingForThis)
        {
            if (travelText != null) travelText.SetActive(false);
            if (giftText != null) giftText.SetActive(true);
            if (giftMessageText != null)
            {
                string itemName = exploreService.PendingItem != null ? exploreService.PendingItem.itemName : "?";
                giftMessageText.text = $"Hi, I found this {itemName}, hope it would be useful for you.";
            }
            if (itemImage != null)
            {
                itemImage.gameObject.SetActive(true);
                itemImage.sprite = exploreService.PendingItem != null ? exploreService.PendingItem.icon : null;
                itemImage.color = Color.white;
            }
            if (itemImageButton != null) itemImageButton.gameObject.SetActive(true);
        }
        else
        {
            if (travelText != null) travelText.SetActive(traveling);
            if (giftText != null) giftText.SetActive(false);
            if (itemImage != null) itemImage.gameObject.SetActive(false);
            if (itemImageButton != null) itemImageButton.gameObject.SetActive(false);
        }
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

    private void OnExploreGiftArrived(ItemDataSO item, string definitionId)
    {
        if (definitionId == _boundDefinitionId)
            RefreshOfficialTexts();
    }

    private void OnExploreGiftClaimed()
    {
        RefreshOfficialTexts();
    }

    private void OnGiftItemClicked()
    {
        var exploreService = ZombieExploreService.Instance;
        if (exploreService == null || !exploreService.HasPendingGift || exploreService.PendingItem == null)
            return;

        ItemDataSO item = exploreService.PendingItem;

        if (itemDisplayCanvasPrefab == null)
        {
            var prefab = Resources.Load<GameObject>("ItemDisplayCanvas");
            if (prefab == null)
                prefab = Resources.Load<GameObject>("UI/ItemDisplayCanvas");
            if (prefab != null)
                itemDisplayCanvasPrefab = prefab;
        }

        if (itemDisplayCanvasPrefab != null)
        {
            GameObject uiInstance = Instantiate(itemDisplayCanvasPrefab);
            var displayUI = uiInstance != null
                ? uiInstance.GetComponent<ItemDisplayUI>() ?? uiInstance.GetComponentInChildren<ItemDisplayUI>(true)
                : null;
            if (displayUI != null)
            {
                displayUI.ShowItem(item.icon, item.itemName, item.description, OnGiftItemPopupClosed);
                return;
            }
        }

        OnGiftItemPopupClosed();
    }

    private void OnGiftItemPopupClosed()
    {
        var exploreService = ZombieExploreService.Instance;
        if (exploreService == null || exploreService.PendingItem == null)
            return;

        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        if (inventory == null)
            return;

        bool added = inventory.AddItem(exploreService.PendingItem);
        if (added)
            exploreService.ClaimGift();
        else
            Debug.Log("[ZombieAssignmentController] Backpack full, item stays pending.");

        RefreshOfficialTexts();
    }
}
