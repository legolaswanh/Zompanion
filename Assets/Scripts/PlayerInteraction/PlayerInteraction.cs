using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Animator Params")]
    [SerializeField] private string digTrigger = "Dig";
    [SerializeField] private string collectTrigger = "Collect";
    [SerializeField] private string faceDirection = "FaceDirection";
    private Vector2 lastDir;
    private PlayerMovement playerMovement;

    private Animator animator;
    private InputSystem_Actions playerInput;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerInput = new InputSystem_Actions();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        playerInput.Enable();

        playerInput.Player.Interact.performed += OnDig;
        playerInput.Player.Attack.performed += OnCollect;
    }

    private void OnDisable()
    {
        playerInput.Player.Interact.performed -= OnDig;
        playerInput.Player.Attack.performed -= OnCollect;

        playerInput.Disable();
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        lastDir = playerMovement.LastDir();
    }

    private void OnDig(InputAction.CallbackContext ctx) 
    {
        if (!ctx.performed) return;

        Debug.Log("Pressed E / Dig");
        Dig();
    }

    private void OnCollect(InputAction.CallbackContext ctx) 
    {
        if (!ctx.performed) return;

        Debug.Log("Pressed Enter / Collect");
        Collect();
    }

    void Dig()
    {
        animator.SetTrigger(digTrigger);
        animator.SetFloat(faceDirection, DirToFloat(lastDir));
    }

    void Collect()
    {
        animator.SetTrigger(collectTrigger);
        animator.SetFloat(faceDirection, DirToFloat(lastDir));
    }

    float DirToFloat(Vector2 dir)
    {
        return dir.y != 0 ? (dir.y > 0 ? 1f : 0f) : (dir.x < 0 ? 2f : 3f);
    }
}
