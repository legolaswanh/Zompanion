using Code.Scripts;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; }

    [Header("Animator Params")]
    [SerializeField] private string digTrigger = "Dig";
    [SerializeField] private string faceDirection = "FaceDirection";

    [Header("Inventory")]
    [SerializeField] private InventorySO playerInventory;

    [Header("Player Bark")]
    [SerializeField] public BarkOnIdle barkTrigger;

    private Vector2 lastDir;
    private PlayerMovement playerMovement;
    private Animator animator;
    private InputSystem_Actions playerInput;
    private GameObject currentActiveTrigger;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        animator = GetComponent<Animator>();
        playerInput = new InputSystem_Actions();
        playerMovement = GetComponent<PlayerMovement>();

        ResolveInventoryReference();
    }

    private void OnEnable()
    {
        playerInput.Enable();
        playerInput.Player.Interact.performed += OnInteraction;
    }

    private void OnDisable()
    {
        playerInput.Player.Interact.performed -= OnInteraction;
        playerInput.Disable();
    }

    private void Start()
    {
        ResolveInventoryReference();
    }

    private void Update()
    {
        if (playerMovement != null)
            lastDir = playerMovement.LastDir();
    }

    private void OnInteraction(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        // 调试：确认 E 键是否到达此处，以及被哪个条件拦截
        string triggerName = currentActiveTrigger != null ? currentActiveTrigger.name : "null";
        string triggerTag = currentActiveTrigger != null ? currentActiveTrigger.tag : "-";
        Debug.Log("[PlayerInteraction] E pressed. pause=" + UIPauseState.IsPaused + " trigger=" + triggerName + " tag=" + triggerTag);

        if (UIPauseState.IsPaused)
            return;

        if (currentActiveTrigger == null)
            return;

        switch (currentActiveTrigger.tag)
        {
            case "DiggingPoint":
                Dig(currentActiveTrigger.GetComponent<DiggingTrigger>());
                break;
            case "AssemblyPlatform":
                OpenAssemblyPlatform(currentActiveTrigger.GetComponent<AssemblyPlatform>());
                break;
            case "Chest":
                OpenChest(currentActiveTrigger.GetComponent<ChestInteractTrigger>());
                break;
            case "InteractiveZombie":
                TriggerDialogue(currentActiveTrigger.GetComponent<DialogueInteractTrigger>());
                break;
            default:
                Debug.LogWarning("[PlayerInteraction] 按 E 时未识别的 Tag: " + currentActiveTrigger.tag + "。宝箱需设为 Tag: Chest");
                break;
        }
    }

    private void Dig(DiggingTrigger trigger)
    {
        if (trigger == null)
            return;

        if (playerInventory == null)
        {
            ResolveInventoryReference();
            if (playerInventory == null)
            {
                Debug.LogWarning("[PlayerInteraction] Player inventory is not available.");
                return;
            }
        }

        if (animator != null)
        {
            animator.SetTrigger(digTrigger);
            animator.SetFloat(faceDirection, DirToFloat(lastDir));
        }

        trigger.Interact(playerInventory);
    }

    private void OpenAssemblyPlatform(AssemblyPlatform platform)
    {
        if (platform == null)
            return;

        platform.OpenPlatFormUI();
    }

    private void OpenChest(ChestInteractTrigger chest)
    {
        if (chest == null)
            return;

        chest.Interact();
    }

    private void TriggerDialogue(DialogueInteractTrigger dialogue)
    {
        if (dialogue == null)
            return;

        dialogue.Interact();
    }

    private float DirToFloat(Vector2 dir)
    {
        return dir.y != 0f ? (dir.y > 0f ? 1f : 0f) : (dir.x < 0f ? 2f : 3f);
    }

    public void SetCurrentTrigger(GameObject trigger)
    {
        currentActiveTrigger = trigger;
    }

    public bool IsCurrentTrigger(GameObject trigger)
    {
        return currentActiveTrigger == trigger;
    }

    public void ClearCurrentTrigger(GameObject trigger)
    {
        if (currentActiveTrigger == trigger)
            currentActiveTrigger = null;
    }

    private void ResolveInventoryReference()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInventory == null)
            return;

        playerInventory = GameManager.Instance.PlayerInventory;
    }
}
