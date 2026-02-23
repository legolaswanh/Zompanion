using UnityEngine;

public class ZombiePanelController : MonoBehaviour
{
    [SerializeField] private ZombieManager zombieManager;
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private ZombieDetailPanel detailPanel;

    private int _selectedInstanceId = -1;

    private void OnEnable()
    {
        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        zombieManager.OnZombieListChanged += RefreshView;
        zombieManager.OnCodexChanged += RefreshDetail;

        RefreshView();
    }

    private void OnDisable()
    {
        if (zombieManager == null)
            return;

        zombieManager.OnZombieListChanged -= RefreshView;
        zombieManager.OnCodexChanged -= RefreshDetail;
    }

    private void RefreshView()
    {
        if (entryPrefab == null || entryRoot == null || zombieManager == null)
            return;

        for (int i = entryRoot.childCount - 1; i >= 0; i--)
            Destroy(entryRoot.GetChild(i).gameObject);

        var zombies = zombieManager.Zombies;
        if (zombies.Count == 0)
        {
            _selectedInstanceId = -1;
            RefreshDetail();
            return;
        }

        bool selectedExists = false;
        for (int i = 0; i < zombies.Count; i++)
        {
            ZombieInstanceData zombie = zombies[i];
            ZombieDefinitionSO definition = zombieManager.GetDefinition(zombie.definitionId);

            ZombieEntryView entry = Instantiate(entryPrefab, entryRoot);
            entry.Setup(zombie, definition, HandleSelect, HandleToggleFollow, HandleToggleWork);

            if (zombie.instanceId == _selectedInstanceId)
                selectedExists = true;
        }

        if (!selectedExists)
            _selectedInstanceId = zombies[0].instanceId;

        RefreshDetail();
    }

    private void HandleSelect(int instanceId)
    {
        _selectedInstanceId = instanceId;
        RefreshDetail();
    }

    private void HandleToggleFollow(int instanceId)
    {
        if (zombieManager == null) return;
        if (!zombieManager.TryGetZombie(instanceId, out ZombieInstanceData zombie)) return;

        bool shouldFollow = zombie.state != Zompanion.ZombieSystem.ZombieState.Following;
        zombieManager.SetFollowState(instanceId, shouldFollow);
    }

    private void HandleToggleWork(int instanceId)
    {
        if (zombieManager == null) return;
        if (!zombieManager.TryGetZombie(instanceId, out ZombieInstanceData zombie)) return;

        bool shouldWork = zombie.state != Zompanion.ZombieSystem.ZombieState.Working;
        zombieManager.SetWorkState(instanceId, shouldWork);
    }

    private void RefreshDetail()
    {
        if (detailPanel == null || zombieManager == null)
            return;

        if (_selectedInstanceId < 0 || !zombieManager.TryGetZombie(_selectedInstanceId, out ZombieInstanceData zombie))
        {
            detailPanel.Clear();
            return;
        }

        ZombieDefinitionSO definition = zombieManager.GetDefinition(zombie.definitionId);
        bool storyUnlocked = definition != null && zombieManager.IsStoryUnlocked(definition.StoryId);
        detailPanel.Bind(zombie, definition, storyUnlocked);
    }
}
