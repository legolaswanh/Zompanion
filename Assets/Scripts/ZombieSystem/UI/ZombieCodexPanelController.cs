using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZombieCodexPanelController : MonoBehaviour
{
    [SerializeField] private ZombieManager zombieManager;
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private ZombieDetailPanel detailPanel;

    private string _selectedDefinitionId;
    private bool _initialized;

    private void OnEnable()
    {
        TryInitialize();
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
            if (!IsPointerOverAnyEntryItem())
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
        IReadOnlyList<ZombieDefinitionSO> catalog = zombieManager.CatalogDefinitions;
        for (int i = 0; i < catalog.Count; i++)
        {
            ZombieDefinitionSO definition = catalog[i];
            if (definition == null) continue;

            bool unlocked = zombieManager.IsZombieCodexUnlocked(definition.DefinitionId);
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
            _selectedDefinitionId = null;

        RefreshDetail();
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
        bool storyUnlocked = unlocked && zombieManager.IsStoryUnlocked(definition.StoryId);
        detailPanel.BindDefinition(definition, unlocked, following, storyUnlocked);
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
}
