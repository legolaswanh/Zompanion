using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set;}

    [Header("Animator Params")]
    [SerializeField] private string digTrigger = "Dig";
    // [SerializeField] private string collectTrigger = "Collect";
    [SerializeField] private string faceDirection = "FaceDirection";

    [Header("玩家的背包数据")]
    [SerializeField] private InventorySO playerInventory;
    private Vector2 lastDir;
    private PlayerMovement playerMovement;

    private Animator animator;
    private InputSystem_Actions playerInput;

    // 核心：记录当前玩家踩在哪个挖掘点上
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
    }

    private void OnEnable()
    {
        playerInput.Enable();

        playerInput.Player.Interact.performed += OnInteraction;
        // 暂时应该不需要捡东西的操作
        // playerInput.Player.Attack.performed += OnCollect;
    }

    private void OnDisable()
    {
        playerInput.Player.Interact.performed -= OnInteraction;
        // 暂时应该不需要捡东西的操作
        // playerInput.Player.Attack.performed -= OnCollect;

        playerInput.Disable();
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        lastDir = playerMovement.LastDir();
    }

    private void OnInteraction(InputAction.CallbackContext ctx) 
    {
        if (!ctx.performed) return;

        Debug.Log("Pressed E / Dig");
        
        if(currentActiveTrigger != null) 
        {
            switch (currentActiveTrigger.tag) {
                case "DiggingPoint":
                    Dig(currentActiveTrigger.GetComponent<DiggingTrigger>());
                    break;
                case "AssemblyPlatform":
                    Debug.Log(currentActiveTrigger.name);
                    OpenAssemblyPlatform(currentActiveTrigger.GetComponent<AssemblyPlatform>());
                    break;
                case "InteractiveZombie":
                    Debug.Log(currentActiveTrigger.name);
                    break;
            }

        }
    }

    // 暂时应该不需要捡东西的操作
    // private void OnCollect(InputAction.CallbackContext ctx) 
    // {
    //     if (!ctx.performed) return;

    //     Debug.Log("Pressed Enter / Collect");
    //     Collect();
    // }

    void Dig(DiggingTrigger trigger)
    {
        if(animator != null) 
        {
            animator.SetTrigger(digTrigger);
            animator.SetFloat(faceDirection, DirToFloat(lastDir));
        }

        // 检查是否有可挖掘的目标
        if (currentActiveTrigger != null)
        {
            Debug.Log($"对 {currentActiveTrigger.name} 执行挖掘！");
            // 调用挖掘点的 Interact 方法，把玩家背包数据传过去
            trigger.Interact(playerInventory);
        }
        else
        {
            Debug.Log("附近没有可挖掘的东西。");
        }
    }

    void OpenAssemblyPlatform(AssemblyPlatform platform) 
    {
        if (currentActiveTrigger != null)
        {
            platform.OpenPlatFormUI();
        }
    }

    // 暂时应该不需要捡东西的操作
    // void Collect()
    // {
    //     animator.SetTrigger(collectTrigger);
    //     animator.SetFloat(faceDirection, DirToFloat(lastDir));
    // }

    float DirToFloat(Vector2 dir)
    {
        return dir.y != 0 ? (dir.y > 0 ? 1f : 0f) : (dir.x < 0 ? 2f : 3f);
    }

    // 当玩家进入 Trigger 时，DiggingTrigger 会调用这个
    public void SetCurrentTrigger(GameObject trigger)
    {
        currentActiveTrigger = trigger;
        Debug.Log($"[交互系统] 进入交互点: {trigger.gameObject.name}");
        // 以后可以在这里显示 "按 E 挖掘" 的 UI
    }

    // 当玩家离开 Trigger 时，DiggingTrigger 会调用这个
    public void ClearCurrentTrigger(GameObject trigger)
    {
        // 只清空当前记录的那个，防止重叠时误删
        if (currentActiveTrigger == trigger)
        {
            currentActiveTrigger = null;
            Debug.Log("[交互系统] 离开交互点");
            // 以后可以在这里隐藏 UI
        }
    }
}
