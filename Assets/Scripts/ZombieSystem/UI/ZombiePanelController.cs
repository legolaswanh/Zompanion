using System.Collections.Generic;
using UnityEngine;

public class ZombiePanelController : MonoBehaviour
{
    [SerializeField] private ZombieManager zombieManager;
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private ZombieDetailPanel detailPanel;
    [SerializeField] private Sprite lockedIcon;

    private string _selectedDefinitionId;
    private readonly List<ZombieDefinitionSO> _unlockedDefinitions = new List<ZombieDefinitionSO>();

    private void OnEnable()
    {
        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        zombieManager.OnZombieListChanged += RefreshView;
        zombieManager.OnCodexChanged += RefreshView;

        RefreshView();
    }

    private void OnDisable()
    {
        if (zombieManager == null)
            return;

        zombieManager.OnZombieListChanged -= RefreshView;
        zombieManager.OnCodexChanged -= RefreshView;
    }

    private void RefreshView()
    {
        if (entryPrefab == null || entryRoot == null || zombieManager == null)
            return;

        for (int i = entryRoot.childCount - 1; i >= 0; i--)
            Destroy(entryRoot.GetChild(i).gameObject);

        _unlockedDefinitions.Clear();
        IReadOnlyList<ZombieDefinitionSO> catalog = zombieManager.CatalogDefinitions;
        for (int i = 0; i < catalog.Count; i++)
        {
            ZombieDefinitionSO definition = catalog[i];
            if (definition == null) continue;
            if (!zombieManager.IsZombieCodexUnlocked(definition.DefinitionId)) continue;
            _unlockedDefinitions.Add(definition);
        }

        if (_unlockedDefinitions.Count == 0)
        {
            _selectedDefinitionId = null;
            RefreshDetail();
            return;
        }

        bool selectedExists = false;
        for (int i = 0; i < _unlockedDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _unlockedDefinitions[i];
            string definitionId = definition.DefinitionId;
            bool following = zombieManager.IsDefinitionFollowing(definitionId);
            bool selected = definitionId == _selectedDefinitionId;

            ZombieEntryView entry = Instantiate(entryPrefab, entryRoot);
            entry.SetupDefinition(
                definition,
                unlocked: true,
                isFollowing: following,
                selected: selected,
                lockedIcon: lockedIcon,
                onSelect: HandleSelect,
                onToggleFollow: HandleToggleFollow);

            if (selected)
                selectedExists = true;
        }

        if (!selectedExists)
            _selectedDefinitionId = _unlockedDefinitions[0].DefinitionId;

        RefreshDetail();
    }

    private void HandleSelect(string definitionId)
    {
        _selectedDefinitionId = definitionId;
        RefreshDetail();
    }

    private void HandleToggleFollow(string definitionId)
    {
        if (zombieManager == null) return;
        zombieManager.TryToggleFollowByDefinitionId(definitionId);
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
        bool following = zombieManager.IsDefinitionFollowing(definition.DefinitionId);
        bool storyUnlocked = unlocked && zombieManager.IsStoryUnlocked(definition.StoryId);
        detailPanel.BindDefinition(definition, unlocked, following, storyUnlocked, lockedIcon);
    }
}

public class ZombieCodexPanelController : MonoBehaviour
{
    [SerializeField] private ZombieManager zombieManager;
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private ZombieDetailPanel detailPanel;
    [SerializeField] private Sprite lockedIcon;

    private string _selectedDefinitionId;

    private void OnEnable()
    {
        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        zombieManager.OnCodexChanged += RefreshView;
        zombieManager.OnZombieListChanged += RefreshView;

        RefreshView();
    }

    private void OnDisable()
    {
        if (zombieManager == null)
            return;

        zombieManager.OnCodexChanged -= RefreshView;
        zombieManager.OnZombieListChanged -= RefreshView;
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
                lockedIcon: lockedIcon,
                onSelect: HandleSelect,
                onToggleFollow: null);

            if (selected)
                selectedExists = true;
        }

        if (!selectedExists)
        {
            _selectedDefinitionId = null;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] == null) continue;
                _selectedDefinitionId = catalog[i].DefinitionId;
                break;
            }
        }

        RefreshDetail();
    }

    private void HandleSelect(string definitionId)
    {
        _selectedDefinitionId = definitionId;
        RefreshDetail();
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
        detailPanel.BindDefinition(definition, unlocked, following, storyUnlocked, lockedIcon);
    }
}
