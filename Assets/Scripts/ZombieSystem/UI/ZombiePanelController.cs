using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombiePanelController : MonoBehaviour
{
    private const int MinJobSlotCount = 1;
    private const int MaxJobSlotCount = 4;

    private enum JobRuntimeMode
    {
        None = 0,
        Follow = 1
    }

    [Serializable]
    private class JobDefinition
    {
        public string jobId = "follow";
        public string displayName = "FOLLOW";
        public Sprite icon;
        [Range(MinJobSlotCount, MaxJobSlotCount)]
        public int slotCount = 2;
        public JobRuntimeMode runtimeMode = JobRuntimeMode.None;
    }

    [Header("Data")]
    [SerializeField] private ZombieManager zombieManager;

    [Header("Jobs")]
    [SerializeField] private List<JobDefinition> jobDefinitions = new List<JobDefinition>();
    [SerializeField] private bool useManagerFollowSlotCount = true;

    [Header("Job List")]
    [SerializeField] private ZombieJobListItemView jobItemPrefab;
    [SerializeField] private Transform jobListRoot;

    [Header("Job Detail")]
    [SerializeField] private GameObject jobDetailPanel;
    [SerializeField] private TMP_Text selectedJobNameText;
    [SerializeField] private ZombieJobSlotView slotItemPrefab;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private Button confirmButton;

    [Header("Candidate List")]
    [SerializeField] private ZombieEntryView entryPrefab;
    [SerializeField] private Transform entryRoot;

    private readonly Dictionary<string, string[]> _committedAssignmentsByJob = new Dictionary<string, string[]>();
    private readonly List<ZombieDefinitionSO> _candidateDefinitions = new List<ZombieDefinitionSO>();

    private string _selectedJobId;
    private string[] _editingAssignments;
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

        DiscardEditingChanges();
        _selectedJobId = null;
        _initialized = false;
        BindButtons(false);
    }

    public void ConfirmSelection()
    {
        ConfirmSelectedJob();
    }

    // Backward-compatible wrappers for old button bindings.
    public void SelectSlotA()
    {
        SelectSlot(0);
    }

    public void SelectSlotB()
    {
        SelectSlot(1);
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
            ClearSlotAssignment(_selectedSlotIndex);

        if (HasValidSelectedSlot() && Input.GetMouseButtonDown(0) && !IsPointerInsideEditingArea())
            ClearSlotEditingSelection();
    }

    private void TryInitialize()
    {
        if (_initialized)
            return;

        if (zombieManager == null)
            zombieManager = ZombieManager.Instance;

        if (zombieManager == null)
            return;

        EnsureJobDefinitions();

        zombieManager.OnZombieListChanged += RefreshView;
        zombieManager.OnCodexChanged += RefreshView;

        DiscardEditingChanges();
        _selectedJobId = null;
        SyncCommittedAssignmentsFromRuntime();
        RefreshView();
        _initialized = true;
    }

    private void EnsureJobDefinitions()
    {
        if (jobDefinitions == null)
            jobDefinitions = new List<JobDefinition>();

        if (jobDefinitions.Count == 0)
        {
            jobDefinitions.Add(new JobDefinition
            {
                jobId = "follow",
                displayName = "FOLLOW",
                slotCount = 2,
                runtimeMode = JobRuntimeMode.Follow
            });
        }

        var usedIds = new HashSet<string>();
        for (int i = 0; i < jobDefinitions.Count; i++)
        {
            JobDefinition job = jobDefinitions[i];
            if (job == null)
            {
                job = new JobDefinition();
                jobDefinitions[i] = job;
            }

            job.slotCount = Mathf.Clamp(job.slotCount, MinJobSlotCount, MaxJobSlotCount);
            if (string.IsNullOrWhiteSpace(job.displayName))
                job.displayName = $"JOB {i + 1}";

            string baseId = string.IsNullOrWhiteSpace(job.jobId) ? $"job_{i + 1}" : job.jobId.Trim();
            string resolvedId = baseId;
            int suffix = 2;
            while (!usedIds.Add(resolvedId))
            {
                resolvedId = $"{baseId}_{suffix}";
                suffix++;
            }

            job.jobId = resolvedId;
        }
    }

    private void RefreshView()
    {
        if (_suppressRefresh)
            return;

        if (zombieManager == null)
            return;

        EnsureJobDefinitions();
        SyncCommittedAssignmentsFromRuntime();
        EnsureSelectedJobValidity();
        EnsureEditingAssignmentsSize();
        BuildCandidateDefinitions();
        EnsureCandidateSelectionValid();

        RefreshJobList();
        RefreshJobDetailPanel();
        RefreshCandidateList();
        RefreshConfirmButton();
    }

    private void SyncCommittedAssignmentsFromRuntime()
    {
        if (zombieManager == null)
            return;

        var validJobIds = new HashSet<string>();
        for (int i = 0; i < jobDefinitions.Count; i++)
        {
            JobDefinition job = jobDefinitions[i];
            if (job == null)
                continue;

            validJobIds.Add(job.jobId);
            int slotCount = GetEffectiveSlotCount(job);
            string[] committed = GetOrCreateCommittedAssignments(job.jobId, slotCount);

            if (job.runtimeMode == JobRuntimeMode.Follow)
            {
                Array.Clear(committed, 0, committed.Length);
                List<string> following = zombieManager.GetFollowingDefinitionIdsOrdered();
                for (int slot = 0; slot < slotCount && slot < following.Count; slot++)
                    committed[slot] = following[slot];
            }
        }

        var staleIds = new List<string>();
        foreach (var pair in _committedAssignmentsByJob)
        {
            if (!validJobIds.Contains(pair.Key))
                staleIds.Add(pair.Key);
        }

        for (int i = 0; i < staleIds.Count; i++)
            _committedAssignmentsByJob.Remove(staleIds[i]);
    }

    private string[] GetOrCreateCommittedAssignments(string jobId, int slotCount)
    {
        if (!_committedAssignmentsByJob.TryGetValue(jobId, out string[] current) || current == null)
        {
            current = new string[slotCount];
            _committedAssignmentsByJob[jobId] = current;
            return current;
        }

        if (current.Length == slotCount)
            return current;

        string[] resized = new string[slotCount];
        int copyCount = Mathf.Min(current.Length, slotCount);
        for (int i = 0; i < copyCount; i++)
            resized[i] = current[i];

        _committedAssignmentsByJob[jobId] = resized;
        return resized;
    }

    private void EnsureSelectedJobValidity()
    {
        if (string.IsNullOrWhiteSpace(_selectedJobId))
            return;

        if (GetJobDefinition(_selectedJobId) == null)
        {
            _selectedJobId = null;
            DiscardEditingChanges();
        }
    }

    private void EnsureEditingAssignmentsSize()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null)
        {
            DiscardEditingChanges();
            return;
        }

        int slotCount = GetEffectiveSlotCount(selectedJob);
        if (_editingAssignments == null)
        {
            BeginEditingSelectedJob();
            return;
        }

        if (_editingAssignments.Length == slotCount)
            return;

        string[] resized = new string[slotCount];
        int copyCount = Mathf.Min(_editingAssignments.Length, slotCount);
        for (int i = 0; i < copyCount; i++)
            resized[i] = _editingAssignments[i];

        _editingAssignments = resized;
        if (_selectedSlotIndex >= slotCount)
            _selectedSlotIndex = -1;
    }

    private void BeginEditingSelectedJob()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null)
        {
            DiscardEditingChanges();
            return;
        }

        int slotCount = GetEffectiveSlotCount(selectedJob);
        string[] committed = GetOrCreateCommittedAssignments(selectedJob.jobId, slotCount);
        _editingAssignments = new string[slotCount];
        for (int i = 0; i < slotCount; i++)
            _editingAssignments[i] = committed[i];

        _selectedSlotIndex = -1;
        _selectedDefinitionId = null;
    }

    private void DiscardEditingChanges()
    {
        _editingAssignments = null;
        _selectedSlotIndex = -1;
        _selectedDefinitionId = null;
    }

    private void SelectJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return;

        if (_selectedJobId == jobId)
            return;

        // Requirement: switching jobs discards current unconfirmed edits.
        DiscardEditingChanges();
        _selectedJobId = jobId;
        BeginEditingSelectedJob();
        RefreshView();
    }

    private void RefreshJobList()
    {
        if (jobItemPrefab == null || jobListRoot == null)
            return;

        for (int i = jobListRoot.childCount - 1; i >= 0; i--)
            Destroy(jobListRoot.GetChild(i).gameObject);

        for (int i = 0; i < jobDefinitions.Count; i++)
        {
            JobDefinition job = jobDefinitions[i];
            if (job == null)
                continue;

            int slotCount = GetEffectiveSlotCount(job);
            string[] displayAssignments = GetDisplayAssignments(job.jobId, slotCount);
            int assignedCount = CountAssigned(displayAssignments);
            bool selected = job.jobId == _selectedJobId;

            ZombieJobListItemView item = Instantiate(jobItemPrefab, jobListRoot);
            item.Setup(job.jobId, job.displayName, job.icon, slotCount, assignedCount, selected, SelectJob);
        }
    }

    private void RefreshJobDetailPanel()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        bool hasSelection = selectedJob != null;

        if (jobDetailPanel != null)
            jobDetailPanel.SetActive(hasSelection);

        if (!hasSelection)
        {
            ClearSlotList();
            if (selectedJobNameText != null)
                selectedJobNameText.text = string.Empty;
            return;
        }

        if (selectedJobNameText != null)
            selectedJobNameText.text = selectedJob.displayName;

        RefreshSlots(selectedJob);
    }

    private void RefreshSlots(JobDefinition selectedJob)
    {
        if (slotItemPrefab == null || slotRoot == null)
            return;

        int slotCount = GetEffectiveSlotCount(selectedJob);
        if (_editingAssignments == null || _editingAssignments.Length != slotCount)
            BeginEditingSelectedJob();

        for (int i = slotRoot.childCount - 1; i >= 0; i--)
            Destroy(slotRoot.GetChild(i).gameObject);

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            string definitionId = _editingAssignments != null ? _editingAssignments[slotIndex] : null;
            ZombieDefinitionSO definition = zombieManager.GetDefinition(definitionId);
            bool selected = slotIndex == _selectedSlotIndex;

            ZombieJobSlotView slotView = Instantiate(slotItemPrefab, slotRoot);
            slotView.Setup(slotIndex, definition, selected, SelectSlot, ClearSlotAssignment);
        }
    }

    private void ClearSlotList()
    {
        if (slotRoot == null)
            return;

        for (int i = slotRoot.childCount - 1; i >= 0; i--)
            Destroy(slotRoot.GetChild(i).gameObject);
    }

    private void SelectSlot(int slotIndex)
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null || _editingAssignments == null)
            return;

        int slotCount = GetEffectiveSlotCount(selectedJob);
        if (slotIndex < 0 || slotIndex >= slotCount)
            return;

        _selectedSlotIndex = slotIndex;
        string currentDefinition = _editingAssignments[slotIndex];
        if (!string.IsNullOrWhiteSpace(currentDefinition))
            _selectedDefinitionId = currentDefinition;

        RefreshView();
    }

    private void ClearSlotAssignment(int slotIndex)
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null || _editingAssignments == null)
            return;

        int slotCount = GetEffectiveSlotCount(selectedJob);
        if (slotIndex < 0 || slotIndex >= slotCount)
            return;

        if (string.IsNullOrWhiteSpace(_editingAssignments[slotIndex]))
            return;

        _editingAssignments[slotIndex] = null;
        if (_selectedSlotIndex == slotIndex)
            _selectedDefinitionId = null;

        RefreshView();
    }

    private void ClearSlotEditingSelection()
    {
        _selectedSlotIndex = -1;
        RefreshView();
    }

    private void BuildCandidateDefinitions()
    {
        _candidateDefinitions.Clear();

        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null || _editingAssignments == null || zombieManager == null)
            return;

        string selectedSlotDefinition = HasValidSelectedSlot() ? _editingAssignments[_selectedSlotIndex] : null;
        var reservedDefinitions = BuildReservedDefinitionSet(selectedSlotDefinition);

        IReadOnlyList<ZombieDefinitionSO> catalog = zombieManager.CatalogDefinitions;
        for (int i = 0; i < catalog.Count; i++)
        {
            ZombieDefinitionSO definition = catalog[i];
            if (definition == null)
                continue;

            string definitionId = definition.DefinitionId;
            if (string.IsNullOrWhiteSpace(definitionId))
                continue;
            if (!zombieManager.IsZombieCodexUnlocked(definitionId))
                continue;
            if (zombieManager.IsDefinitionWorking(definitionId))
                continue;
            if (reservedDefinitions.Contains(definitionId))
                continue;

            _candidateDefinitions.Add(definition);
        }
    }

    private HashSet<string> BuildReservedDefinitionSet(string selectedSlotDefinition)
    {
        var reserved = new HashSet<string>();

        for (int jobIndex = 0; jobIndex < jobDefinitions.Count; jobIndex++)
        {
            JobDefinition job = jobDefinitions[jobIndex];
            if (job == null)
                continue;

            int slotCount = GetEffectiveSlotCount(job);
            string[] assignments = GetDisplayAssignments(job.jobId, slotCount);
            if (assignments == null)
                continue;

            for (int slot = 0; slot < assignments.Length; slot++)
            {
                string definitionId = assignments[slot];
                if (string.IsNullOrWhiteSpace(definitionId))
                    continue;

                bool isCurrentSelectedSlot = job.jobId == _selectedJobId && slot == _selectedSlotIndex;
                if (isCurrentSelectedSlot && definitionId == selectedSlotDefinition)
                    continue;

                reserved.Add(definitionId);
            }
        }

        return reserved;
    }

    private void EnsureCandidateSelectionValid()
    {
        if (_candidateDefinitions.Count == 0)
        {
            _selectedDefinitionId = null;
            return;
        }

        for (int i = 0; i < _candidateDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _candidateDefinitions[i];
            if (definition == null)
                continue;

            if (definition.DefinitionId == _selectedDefinitionId)
                return;
        }

        _selectedDefinitionId = _candidateDefinitions[0].DefinitionId;
    }

    private void RefreshCandidateList()
    {
        if (entryPrefab == null || entryRoot == null)
            return;

        for (int i = entryRoot.childCount - 1; i >= 0; i--)
            Destroy(entryRoot.GetChild(i).gameObject);

        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null)
            return;

        for (int i = 0; i < _candidateDefinitions.Count; i++)
        {
            ZombieDefinitionSO definition = _candidateDefinitions[i];
            if (definition == null)
                continue;

            bool selected = definition.DefinitionId == _selectedDefinitionId;
            ZombieEntryView entry = Instantiate(entryPrefab, entryRoot);
            entry.SetupDefinition(
                definition,
                unlocked: true,
                isFollowing: false,
                selected: selected,
                onSelect: HandleSelectCandidate);
        }
    }

    private void HandleSelectCandidate(string definitionId)
    {
        _selectedDefinitionId = definitionId;

        if (HasValidSelectedSlot() && _editingAssignments != null)
            _editingAssignments[_selectedSlotIndex] = definitionId;

        RefreshView();
    }

    private void ConfirmSelectedJob()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null || _editingAssignments == null || zombieManager == null)
            return;

        if (!HasPendingChangesForSelectedJob())
            return;

        _suppressRefresh = true;
        try
        {
            if (selectedJob.runtimeMode == JobRuntimeMode.Follow)
                ApplyFollowAssignments(_editingAssignments);

            string[] committed = GetOrCreateCommittedAssignments(selectedJob.jobId, _editingAssignments.Length);
            for (int i = 0; i < committed.Length; i++)
                committed[i] = _editingAssignments[i];
        }
        finally
        {
            _suppressRefresh = false;
        }

        // Requirement: after confirm, close detail panel and clear current job selection highlight.
        DiscardEditingChanges();
        _selectedJobId = null;
        RefreshView();
    }

    private void ApplyFollowAssignments(string[] targetAssignments)
    {
        if (zombieManager == null || targetAssignments == null)
            return;

        var targetOrdered = new List<string>();
        for (int i = 0; i < targetAssignments.Length; i++)
        {
            string definitionId = targetAssignments[i];
            if (string.IsNullOrWhiteSpace(definitionId))
                continue;
            if (targetOrdered.Contains(definitionId))
                continue;
            targetOrdered.Add(definitionId);
        }

        List<string> currentFollowing = zombieManager.GetFollowingDefinitionIdsOrdered();
        for (int i = 0; i < currentFollowing.Count; i++)
            zombieManager.TrySetFollowByDefinitionId(currentFollowing[i], false);

        int assignCount = Mathf.Min(zombieManager.MaxFollowCount, targetOrdered.Count);
        for (int i = 0; i < assignCount; i++)
            zombieManager.TrySetFollowByDefinitionId(targetOrdered[i], true);
    }

    private bool HasPendingChangesForSelectedJob()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null || _editingAssignments == null)
            return false;

        int slotCount = GetEffectiveSlotCount(selectedJob);
        string[] committed = GetOrCreateCommittedAssignments(selectedJob.jobId, slotCount);
        for (int i = 0; i < slotCount; i++)
        {
            if (committed[i] != _editingAssignments[i])
                return true;
        }

        return false;
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
            return;

        JobDefinition selectedJob = GetSelectedJobDefinition();
        confirmButton.gameObject.SetActive(selectedJob != null);
        confirmButton.interactable = selectedJob != null && HasPendingChangesForSelectedJob();
    }

    private int CountAssigned(string[] assignments)
    {
        if (assignments == null)
            return 0;

        int count = 0;
        for (int i = 0; i < assignments.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(assignments[i]))
                count++;
        }

        return count;
    }

    private string[] GetDisplayAssignments(string jobId, int slotCount)
    {
        if (!string.IsNullOrWhiteSpace(_selectedJobId) &&
            _selectedJobId == jobId &&
            _editingAssignments != null &&
            _editingAssignments.Length == slotCount)
        {
            return _editingAssignments;
        }

        return GetOrCreateCommittedAssignments(jobId, slotCount);
    }

    private JobDefinition GetSelectedJobDefinition()
    {
        return GetJobDefinition(_selectedJobId);
    }

    private JobDefinition GetJobDefinition(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId) || jobDefinitions == null)
            return null;

        for (int i = 0; i < jobDefinitions.Count; i++)
        {
            JobDefinition job = jobDefinitions[i];
            if (job == null)
                continue;
            if (job.jobId == jobId)
                return job;
        }

        return null;
    }

    private int GetEffectiveSlotCount(JobDefinition job)
    {
        if (job == null)
            return MinJobSlotCount;

        int count = Mathf.Clamp(job.slotCount, MinJobSlotCount, MaxJobSlotCount);
        if (job.runtimeMode == JobRuntimeMode.Follow && useManagerFollowSlotCount && zombieManager != null)
            count = Mathf.Clamp(zombieManager.MaxFollowCount, MinJobSlotCount, MaxJobSlotCount);

        return count;
    }

    private bool HasValidSelectedSlot()
    {
        JobDefinition selectedJob = GetSelectedJobDefinition();
        if (selectedJob == null)
            return false;

        int slotCount = GetEffectiveSlotCount(selectedJob);
        return _selectedSlotIndex >= 0 && _selectedSlotIndex < slotCount;
    }

    private bool IsPointerInsideEditingArea()
    {
        if (IsPointerOverButton(confirmButton))
            return true;

        if (IsPointerOverRect(slotRoot as RectTransform))
            return true;

        if (IsPointerOverRect(entryRoot as RectTransform))
            return true;

        return IsPointerOverRect(jobListRoot as RectTransform);
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
        if (confirmButton == null)
            return;

        confirmButton.onClick.RemoveListener(ConfirmSelection);
        if (bind)
            confirmButton.onClick.AddListener(ConfirmSelection);
    }
}
