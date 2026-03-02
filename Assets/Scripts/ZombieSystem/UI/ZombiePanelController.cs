using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombiePanelController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ZombieManager zombieManager;

    [Header("List")]
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;
    [SerializeField] private Sprite lockedIcon;

    [Header("Slots")]
    [SerializeField] private Button slotAButton;
    [SerializeField] private Button slotBButton;
    [SerializeField] private Image slotAImage;
    [SerializeField] private Image slotBImage;
    [SerializeField] private TMP_Text slotAText;
    [SerializeField] private TMP_Text slotBText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject slotASelectedMarker;
    [SerializeField] private GameObject slotBSelectedMarker;

    private readonly List<ZombieDefinitionSO> _candidateDefinitions = new List<ZombieDefinitionSO>();
    private readonly string[] _slotDefinitionIds = new string[2];
    private readonly string[] _pendingSlotDefinitionIds = new string[2];

    private int _selectedSlotIndex = -1;
    private string _selectedDefinitionId;
    private bool _suppressRefresh;
    private bool _initialized;

    private void OnEnable()
    {
        BindButtons(true);
        TryInitialize();
    }

    private void OnDisable()
    {
        if (_initialized && zombieManager != null)
        {
            zombieManager.OnZombieListChanged -= RefreshView;
            zombieManager.OnCodexChanged -= RefreshView;
        }

        _initialized = false;
        BindButtons(false);
    }

    public void SelectSlotA()
    {
        SelectSlot(0);
    }

    public void SelectSlotB()
    {
        SelectSlot(1);
    }

    public void ConfirmSelection()
    {
        ConfirmSelectedSlot();
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

        if (!_initialized || zombieManager == null)
            return;

        if (HasValidSelectedSlot() && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
            DeleteSlotAssignment(_selectedSlotIndex);

        if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverButton(slotAButton))
                DeleteSlotAssignment(0);
            else if (IsPointerOverButton(slotBButton))
                DeleteSlotAssignment(1);
        }

        if (HasValidSelectedSlot() && Input.GetMouseButtonDown(0) && !IsPointerInsideSelectionArea())
            ClearEditingSelection();
    }

    private void TryInitialize()
    {
        if (_initialized)
            return;

        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        zombieManager.OnZombieListChanged += RefreshView;
        zombieManager.OnCodexChanged += RefreshView;

        _selectedSlotIndex = -1;
        _selectedDefinitionId = null;
        SyncCommittedSlotsFromManager();
        CopyCommittedSlotsToPending();
        RefreshView();
        _initialized = true;
    }

    private void SelectSlot(int slotIndex)
    {
        int slotCount = GetSlotCount();
        if (slotCount <= 0)
            return;

        _selectedSlotIndex = Mathf.Clamp(slotIndex, 0, slotCount - 1);

        string slotDefinition = _pendingSlotDefinitionIds[_selectedSlotIndex];
        if (!string.IsNullOrWhiteSpace(slotDefinition))
            _selectedDefinitionId = slotDefinition;

        RefreshView();
    }

    private void ConfirmSelectedSlot()
    {
        if (zombieManager == null)
            return;

        if (!HasAnyPendingChanges())
            return;

        ApplyPendingAssignments();
        SyncCommittedSlotsFromManager();
        CopyCommittedSlotsToPending();
        ClearEditingSelection();
        RefreshView();
    }

    private void HandleSelectCandidate(string definitionId)
    {
        _selectedDefinitionId = definitionId;

        if (HasValidSelectedSlot())
            _pendingSlotDefinitionIds[_selectedSlotIndex] = definitionId;

        RefreshView();
    }

    private void RefreshView()
    {
        if (_suppressRefresh)
            return;

        if (entryPrefab == null || entryRoot == null || zombieManager == null)
            return;

        SyncCommittedSlotsFromManager();
        BuildCandidateDefinitions();
        EnsureSelectionValid();
        RebuildEntryList();
        RefreshSlotUi();
    }

    private void SyncCommittedSlotsFromManager()
    {
        for (int i = 0; i < _slotDefinitionIds.Length; i++)
            _slotDefinitionIds[i] = null;

        List<string> followingDefinitions = zombieManager.GetFollowingDefinitionIdsOrdered();
        int slotCount = GetSlotCount();
        for (int i = 0; i < slotCount && i < followingDefinitions.Count; i++)
            _slotDefinitionIds[i] = followingDefinitions[i];

        if (_selectedSlotIndex >= slotCount)
            _selectedSlotIndex = -1;
    }

    private void CopyCommittedSlotsToPending()
    {
        for (int i = 0; i < _pendingSlotDefinitionIds.Length; i++)
            _pendingSlotDefinitionIds[i] = _slotDefinitionIds[i];
    }

    private void BuildCandidateDefinitions()
    {
        _candidateDefinitions.Clear();

        if (zombieManager == null)
            return;

        IReadOnlyList<ZombieDefinitionSO> catalog = zombieManager.CatalogDefinitions;
        for (int i = 0; i < catalog.Count; i++)
        {
            ZombieDefinitionSO definition = catalog[i];
            if (definition == null) continue;

            string definitionId = definition.DefinitionId;
            if (!zombieManager.IsZombieCodexUnlocked(definitionId)) continue;
            if (zombieManager.IsDefinitionWorking(definitionId)) continue;
            if (IsDefinitionAssignedInPending(definitionId)) continue;

            _candidateDefinitions.Add(definition);
        }
    }

    private void EnsureSelectionValid()
    {
        if (_candidateDefinitions.Count == 0)
        {
            _selectedDefinitionId = null;
            return;
        }

        if (!ContainsCandidate(_selectedDefinitionId))
            _selectedDefinitionId = _candidateDefinitions[0].DefinitionId;
    }

    private void RebuildEntryList()
    {
        if (entryPrefab == null || entryRoot == null)
            return;

        for (int i = entryRoot.childCount - 1; i >= 0; i--)
            Destroy(entryRoot.GetChild(i).gameObject);

        for (int i = 0; i < _candidateDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _candidateDefinitions[i];
            string definitionId = definition.DefinitionId;
            bool selected = definitionId == _selectedDefinitionId;
            bool following = false;

            ZombieEntryView entry = Instantiate(entryPrefab, entryRoot);
            entry.SetupDefinition(
                definition,
                unlocked: true,
                isFollowing: following,
                selected: selected,
                lockedIcon: lockedIcon,
                onSelect: HandleSelectCandidate);
        }
    }

    private void RefreshSlotUi()
    {
        int slotCount = GetSlotCount();

        if (slotAButton != null)
            slotAButton.interactable = slotCount > 0;
        if (slotBButton != null)
            slotBButton.gameObject.SetActive(slotCount > 1);

        string slotADefinition = _pendingSlotDefinitionIds[0];
        string slotBDefinition = _pendingSlotDefinitionIds[1];

        if (slotAText != null)
            slotAText.text = $"Slot A: {GetDefinitionDisplayName(slotADefinition)}";
        if (slotBText != null)
            slotBText.text = $"Slot B: {(slotCount > 1 ? GetDefinitionDisplayName(slotBDefinition) : "-")}";

        RefreshSlotIcon(slotAImage, slotADefinition);
        RefreshSlotIcon(slotBImage, slotCount > 1 ? slotBDefinition : null);

        if (slotASelectedMarker != null)
            slotASelectedMarker.SetActive(HasValidSelectedSlot() && _selectedSlotIndex == 0);

        if (slotBSelectedMarker != null)
            slotBSelectedMarker.SetActive(HasValidSelectedSlot() && _selectedSlotIndex == 1);

        if (confirmButton != null)
        {
            confirmButton.interactable = HasAnyPendingChanges();
        }
    }

    private int GetSlotCount()
    {
        if (zombieManager == null)
            return 1;

        return Mathf.Clamp(zombieManager.MaxFollowCount, 1, _slotDefinitionIds.Length);
    }

    private bool ContainsCandidate(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        for (int i = 0; i < _candidateDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _candidateDefinitions[i];
            if (definition == null) continue;
            if (definition.DefinitionId == definitionId)
                return true;
        }

        return false;
    }

    private string GetDefinitionDisplayName(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return "Empty";

        ZombieDefinitionSO definition = zombieManager != null ? zombieManager.GetDefinition(definitionId) : null;
        return definition != null ? definition.DisplayName : definitionId;
    }

    private bool IsDefinitionAssignedInPending(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return false;

        int slotCount = GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            if (_pendingSlotDefinitionIds[i] == definitionId)
                return true;
        }

        return false;
    }

    private void RefreshSlotIcon(Image targetImage, string definitionId)
    {
        if (targetImage == null)
            return;

        ZombieDefinitionSO definition = zombieManager != null ? zombieManager.GetDefinition(definitionId) : null;
        Sprite icon = definition != null ? definition.CodexIcon : null;

        targetImage.sprite = icon;
        Color color = targetImage.color;
        color.a = icon != null ? 1f : 0f;
        targetImage.color = color;
    }

    private void DeleteSlotAssignment(int slotIndex)
    {
        if (zombieManager == null)
            return;

        int slotCount = GetSlotCount();
        if (slotIndex < 0 || slotIndex >= slotCount)
            return;

        string definitionId = _slotDefinitionIds[slotIndex];
        if (string.IsNullOrWhiteSpace(definitionId))
            return;

        _selectedSlotIndex = slotIndex;
        _pendingSlotDefinitionIds[slotIndex] = null;
        RefreshView();
    }

    private void ClearEditingSelection()
    {
        _selectedSlotIndex = -1;
        RefreshSlotUi();
    }

    private bool HasAnyPendingChanges()
    {
        int slotCount = GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            if (_pendingSlotDefinitionIds[i] != _slotDefinitionIds[i])
                return true;
        }

        return false;
    }

    private void ApplyPendingAssignments()
    {
        int slotCount = GetSlotCount();
        var currentDefinitions = new string[slotCount];
        var targetDefinitions = new string[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            currentDefinitions[i] = _slotDefinitionIds[i];
            targetDefinitions[i] = _pendingSlotDefinitionIds[i];
        }

        var targetDefinitionSet = new HashSet<string>();
        for (int i = 0; i < slotCount; i++)
        {
            string pendingId = targetDefinitions[i];
            if (!string.IsNullOrWhiteSpace(pendingId))
                targetDefinitionSet.Add(pendingId);
        }

        _suppressRefresh = true;
        try
        {
            for (int i = 0; i < slotCount; i++)
            {
                string currentId = currentDefinitions[i];
                if (string.IsNullOrWhiteSpace(currentId)) continue;
                if (targetDefinitionSet.Contains(currentId)) continue;
                zombieManager.TrySetFollowByDefinitionId(currentId, false);
            }

            for (int i = 0; i < slotCount; i++)
            {
                string targetId = targetDefinitions[i];
                if (string.IsNullOrWhiteSpace(targetId)) continue;
                if (zombieManager.IsDefinitionFollowing(targetId)) continue;
                zombieManager.TrySetFollowByDefinitionId(targetId, true);
            }
        }
        finally
        {
            _suppressRefresh = false;
        }
    }

    private bool IsPointerInsideSelectionArea()
    {
        if (IsPointerOverButton(slotAButton) || IsPointerOverButton(slotBButton) || IsPointerOverButton(confirmButton))
            return true;

        RectTransform entryRect = entryRoot as RectTransform;
        return entryRect != null && IsPointerOverRect(entryRect);
    }

    private bool HasValidSelectedSlot()
    {
        int slotCount = GetSlotCount();
        return _selectedSlotIndex >= 0 && _selectedSlotIndex < slotCount;
    }

    private bool IsPointerOverButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            return false;

        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
            return false;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
    }

    private bool IsPointerOverRect(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
    }

    private void BindButtons(bool bind)
    {
        if (slotAButton != null)
        {
            slotAButton.onClick.RemoveListener(SelectSlotA);
            if (bind) slotAButton.onClick.AddListener(SelectSlotA);
        }

        if (slotBButton != null)
        {
            slotBButton.onClick.RemoveListener(SelectSlotB);
            if (bind) slotBButton.onClick.AddListener(SelectSlotB);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmSelection);
            if (bind) confirmButton.onClick.AddListener(ConfirmSelection);
        }
    }
}
