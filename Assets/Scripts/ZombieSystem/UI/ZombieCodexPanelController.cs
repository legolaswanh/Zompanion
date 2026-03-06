using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZombieCodexPanelController : MonoBehaviour
{
    [SerializeField] private ZombieManager zombieManager;
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private ZombieDetailPanel detailPanel;
    [Tooltip("可选：用于判断「点击不清除选中」的右侧区域。若不填则用 detailPanel 的 Rect")]
    [SerializeField] private RectTransform detailAreaRect;

    [Header("Page Navigation")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [Tooltip("信息页元素：Portrait、Name、Description 等，Page 0 时显示")]
    [SerializeField] private RectTransform[] infoPageElements;
    [Tooltip("剧情页容器：ZombieStoryPanel，Page 1 时显示")]
    [SerializeField] private RectTransform storyPageRoot;
    [Tooltip("僵尸列表容器：ScrollView，Page 1 时隐藏，避免与剧情页重叠")]
    [SerializeField] private RectTransform zombieListRoot;

    private string _selectedDefinitionId;
    private string _pendingUnlockAnimationDefinitionId;
    private bool _initialized;
    private int _currentPage; // 0 = 信息页, 1 = 剧情页

    private void OnEnable()
    {
        TryInitialize();
        ApplyPageVisibility();
    }

    private void OnDisable()
    {
        if (_initialized && zombieManager != null)
        {
            zombieManager.OnCodexChanged -= RefreshView;
            zombieManager.OnZombieListChanged -= RefreshView;
        }

        _initialized = false;
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (!_initialized || zombieManager == null)
        {
            if (_initialized && zombieManager == null)
                _initialized = false;

            TryInitialize();
        }

        if (_initialized && Input.GetMouseButtonDown(0) && !string.IsNullOrWhiteSpace(_selectedDefinitionId))
        {
            if (!IsPointerOverAnyEntryItem() && !IsPointerOverDetailPanel())
                ClearSelection();
        }
    }

    private void TryInitialize()
    {
        if (_initialized)
            return;

        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        zombieManager.OnCodexChanged += RefreshView;
        zombieManager.OnZombieListChanged += RefreshView;

        _selectedDefinitionId = null;
        _currentPage = 0;
        SetupArrowButtons();
        RefreshView();
        _initialized = true;
    }

    private void RefreshView()
    {
        if (entryPrefab == null || entryRoot == null || zombieManager == null)
            return;

        for (int i = entryRoot.childCount - 1; i >= 0; i--)
            Destroy(entryRoot.GetChild(i).gameObject);

        bool selectedExists = false;
        string firstUnlockedId = null;
        IReadOnlyList<ZombieDefinitionSO> catalog = zombieManager.CatalogDefinitions;
        for (int i = 0; i < catalog.Count; i++)
        {
            ZombieDefinitionSO definition = catalog[i];
            if (definition == null) continue;

            bool unlocked = zombieManager.IsZombieCodexUnlocked(definition.DefinitionId);
            if (unlocked && string.IsNullOrEmpty(firstUnlockedId))
                firstUnlockedId = definition.DefinitionId;

            bool following = unlocked && zombieManager.IsDefinitionFollowing(definition.DefinitionId);
            bool selected = definition.DefinitionId == _selectedDefinitionId;

            ZombieEntryView entry = Instantiate(entryPrefab, entryRoot);
            entry.SetupDefinition(
                definition,
                unlocked,
                isFollowing: following,
                selected: selected,
                onSelect: HandleSelect);

            if (selected)
                selectedExists = true;
        }

        if (!selectedExists)
        {
            _selectedDefinitionId = null;
            _currentPage = 0;
            if (!string.IsNullOrEmpty(firstUnlockedId))
            {
                _selectedDefinitionId = firstUnlockedId;
                selectedExists = true;
            }
        }

        RefreshDetail();
        ApplyPageVisibility();

        if (!string.IsNullOrWhiteSpace(_pendingUnlockAnimationDefinitionId))
        {
            for (int i = 0; i < entryRoot.childCount; i++)
            {
                ZombieEntryView entry = entryRoot.GetChild(i).GetComponent<ZombieEntryView>();
                if (entry != null && entry.DefinitionId == _pendingUnlockAnimationDefinitionId)
                {
                    entry.PlayUnlockAnimation();
                    break;
                }
            }
            _pendingUnlockAnimationDefinitionId = null;
        }
    }

    /// <summary>
    /// 选中指定僵尸并播放解锁动画，用于组装完成后打开图鉴聚焦新僵尸。
    /// </summary>
    /// <param name="goToStoryPage">true 时直接切换到剧情页（用于提交道具解锁 story 后）</param>
    public void SelectAndShowNewZombie(string definitionId, bool goToStoryPage = false)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return;

        _selectedDefinitionId = definitionId;
        _pendingUnlockAnimationDefinitionId = definitionId;
        if (goToStoryPage)
            _currentPage = 1;
        TryInitialize();
        RefreshView();
    }

    private void HandleSelect(string definitionId)
    {
        _selectedDefinitionId = definitionId;
        RefreshView();
    }

    private void ClearSelection()
    {
        if (string.IsNullOrWhiteSpace(_selectedDefinitionId))
            return;

        _selectedDefinitionId = null;
        RefreshView();
    }

    private void RefreshDetail()
    {
        if (detailPanel == null || zombieManager == null)
            return;

        if (string.IsNullOrWhiteSpace(_selectedDefinitionId))
        {
            detailPanel.Clear();
            return;
        }

        ZombieDefinitionSO definition = zombieManager.GetDefinition(_selectedDefinitionId);
        if (definition == null)
        {
            detailPanel.Clear();
            return;
        }

        bool unlocked = zombieManager.IsZombieCodexUnlocked(definition.DefinitionId);
        bool following = unlocked && zombieManager.IsDefinitionFollowing(definition.DefinitionId);
        bool storyUnlocked = unlocked && zombieManager.AreAllStoriesUnlockedForZombie(definition.DefinitionId);
        detailPanel.BindDefinition(definition, unlocked, following, storyUnlocked, zombieManager);

        if (_currentPage == 1)
            detailPanel.BindStoryPage(definition, unlocked, zombieManager);
    }

    private void SetupArrowButtons()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(() => GoToPage(0));
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(() => GoToPage(1));
        }

        ApplyPageVisibility();
    }

    private void GoToPage(int page)
    {
        _currentPage = Mathf.Clamp(page, 0, 1);
        ApplyPageVisibility();

        if (detailPanel != null && zombieManager != null && !string.IsNullOrWhiteSpace(_selectedDefinitionId))
        {
            ZombieDefinitionSO definition = zombieManager.GetDefinition(_selectedDefinitionId);
            if (definition != null && _currentPage == 1)
            {
                bool unlocked = zombieManager.IsZombieCodexUnlocked(definition.DefinitionId);
                detailPanel.BindStoryPage(definition, unlocked, zombieManager);
            }
        }
    }

    private void ApplyPageVisibility()
    {
        bool onInfoPage = _currentPage == 0;
        bool hasSelection = !string.IsNullOrWhiteSpace(_selectedDefinitionId);

        if (leftArrowButton != null)
            leftArrowButton.gameObject.SetActive(!onInfoPage);

        if (rightArrowButton != null)
            rightArrowButton.gameObject.SetActive(onInfoPage && hasSelection);

        if (infoPageElements != null)
        {
            foreach (var rt in infoPageElements)
            {
                if (rt != null)
                    rt.gameObject.SetActive(onInfoPage && hasSelection);
            }
        }

        if (storyPageRoot != null)
            storyPageRoot.gameObject.SetActive(!onInfoPage);

        if (zombieListRoot != null)
            zombieListRoot.gameObject.SetActive(onInfoPage);
    }

    private bool IsPointerOverAnyEntryItem()
    {
        if (entryRoot == null || !entryRoot.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = entryRoot.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        for (int i = 0; i < entryRoot.childCount; i++)
        {
            RectTransform childRect = entryRoot.GetChild(i) as RectTransform;
            if (childRect == null || !childRect.gameObject.activeInHierarchy)
                continue;

            Selectable selectable = childRect.GetComponentInChildren<Selectable>(true);
            if (selectable == null || !selectable.interactable)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(childRect, Input.mousePosition, eventCamera))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 点击右侧详情区（codex_pannel_right）时不清除选中。
    /// </summary>
    private bool IsPointerOverDetailPanel()
    {
        RectTransform checkRect = detailAreaRect != null ? detailAreaRect : (detailPanel != null ? detailPanel.transform as RectTransform : null);
        if (checkRect == null || !checkRect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = checkRect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(checkRect, Input.mousePosition, eventCamera);
    }
}
