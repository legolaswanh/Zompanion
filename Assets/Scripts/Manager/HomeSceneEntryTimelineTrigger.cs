using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Code.Scripts;

/// <summary>
/// 仅用于 HomeScene：玩家背包有 A Mysterious Book 时，
/// 进入后禁用移动，物品格闪烁，任何操作弹出提示，点击该物品播放 PlatformTimeline。
/// </summary>
public class HomeSceneEntryTimelineTrigger : MonoBehaviour
{
    [SerializeField] PlayableDirector platformTimeline;
    [SerializeField] string requiredItemName = "A Mysterious Book";
    [SerializeField] string blockMessage = "The book in my bag is too heavy to carry";
    [SerializeField] InventoryDisplay inventoryDisplay;
    [SerializeField] float flashScaleMin = 0.9f;
    [SerializeField] float flashScaleMax = 1.15f;
    [SerializeField] float flashSpeed = 3f;

    [Header("Debug")]
    [SerializeField] bool enableLog;

    /// <summary>本局游戏是否已触发过 Timeline，进程重启时自动重置，无需 F5 清理。</summary>
    static bool _sessionTimelinePlayed;

    void Log(string msg) { if (enableLog) Debug.Log($"[HomeSceneMysteryBook] {msg}"); }

    ItemDataSO _requiredItem;
    bool _isHeavyState;
    Coroutine _flashCoroutine;
    InputSystem_Actions _blockInput;

    void Start()
    {
        StartCoroutine(InitWhenReady());
    }

    void OnDestroy()
    {
        ExitHeavyState();
        if (TimelineManager.Instance != null)
            TimelineManager.Instance.OnTimelineFinished -= OnTimelineFinished;
    }

    IEnumerator InitWhenReady()
    {
        yield return null;

        if (platformTimeline == null)
        {
            Log("platformTimeline 为空，退出");
            yield break;
        }

        while (GameManager.Instance == null || GameManager.Instance.PlayerInventory == null)
            yield return null;

        yield return null;

        if (inventoryDisplay == null)
            inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();

        _requiredItem = ItemLookup.Get(requiredItemName);
        if (_requiredItem == null)
        {
            Log($"未找到物品: {requiredItemName}");
            yield break;
        }

        var inventory = GameManager.Instance.PlayerInventory;
        bool hasBook = inventory.Slots.Any(s => s != null && !s.IsEmpty && s.itemData == _requiredItem);
        if (!hasBook)
        {
            Log($"背包中没有 {requiredItemName}");
            yield break;
        }

        if (_sessionTimelinePlayed)
        {
            Log("本局已播放过 Timeline，跳过");
            yield break;
        }

        Log("进入 heavy 状态");
        EnterHeavyState();
        TimelineManager.Instance.OnTimelineFinished += OnTimelineFinished;
    }

    void Update()
    {
        if (!_isHeavyState) return;
    }

    void EnterHeavyState()
    {
        _isHeavyState = true;
        GameManager.Instance?.SetPlayerControlEnabled(false);
        StartSlotFlash();

        _blockInput = new InputSystem_Actions();
        _blockInput.Player.Move.performed += OnBlockedInput;
        _blockInput.Player.Interact.performed += OnBlockedInput;
        _blockInput.Enable();
    }

    void OnBlockedInput(InputAction.CallbackContext _)
    {
        if (_isHeavyState)
            ShowBlockMessage();
    }

    void ExitHeavyState()
    {
        _isHeavyState = false;
        if (_blockInput != null)
        {
            _blockInput.Player.Move.performed -= OnBlockedInput;
            _blockInput.Player.Interact.performed -= OnBlockedInput;
            _blockInput.Disable();
            _blockInput = null;
        }
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
        StopSlotFlash();
        GameManager.Instance?.SetPlayerControlEnabled(true);
    }

    void StartSlotFlash()
    {
        _flashCoroutine = StartCoroutine(SlotFlashCoroutine());
    }

    void StopSlotFlash()
    {
        if (inventoryDisplay == null) return;
        var slot = inventoryDisplay.TryGetSlotUIForItem(_requiredItem);
        var iconRt = GetIconRect(slot);
        if (iconRt != null) iconRt.localScale = Vector3.one;
    }

    static RectTransform GetIconRect(InventorySlotUI slot)
    {
        if (slot == null) return null;
        var itemUI = slot.GetComponentInChildren<ItemUI>(true);
        return itemUI != null ? itemUI.transform as RectTransform : null;
    }

    IEnumerator SlotFlashCoroutine()
    {
        InventorySlotUI slot = null;
        while (_isHeavyState && inventoryDisplay != null)
        {
            slot = inventoryDisplay.TryGetSlotUIForItem(_requiredItem);
            if (slot == null) yield return new WaitForSeconds(0.2f);
            else break;
        }

        RectTransform iconRt = GetIconRect(slot);
        if (iconRt == null) yield break;

        float t = 0f;
        while (_isHeavyState && iconRt != null)
        {
            t += Time.unscaledDeltaTime * flashSpeed;
            float s = Mathf.Lerp(flashScaleMin, flashScaleMax, (Mathf.Sin(t) + 1f) * 0.5f);
            iconRt.localScale = Vector3.one * s;
            yield return null;
        }
    }

    void ShowBlockMessage()
    {
        var hint = HintPanelController.GetInstance();
        if (hint != null)
            hint.ShowHint(blockMessage, 2f);
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        if (director != platformTimeline || !_isHeavyState) return;
        _sessionTimelinePlayed = true;
        ExitHeavyState();
        TimelineManager.Instance.OnTimelineFinished -= OnTimelineFinished;
    }

    /// <summary>
    /// 由 InventorySlotUI 在点击物品时调用；若处于 heavy 且点击的是目标物品，则播放 Timeline 并消费物品。
    /// </summary>
    public static bool TryPlayPlatformTimelineFromClick(ItemDataSO clickedItem, PlayableDirector director)
    {
        var trigger = FindFirstObjectByType<HomeSceneEntryTimelineTrigger>();
        if (trigger == null || !trigger._isHeavyState) return false;
        if (clickedItem != trigger._requiredItem || director != trigger.platformTimeline) return false;

        _sessionTimelinePlayed = true;
        trigger.ExitHeavyState();
        TimelineManager.Instance.OnTimelineFinished -= trigger.OnTimelineFinished;

        var inv = GameManager.Instance?.PlayerInventory;
        if (inv != null) inv.RemoveItem(clickedItem);

        if (TimelineManager.Instance != null)
            TimelineManager.Instance.Play(director, bindPlayer: false, manageVCams: true);
        else
            director.Play();

        return true;
    }
}
