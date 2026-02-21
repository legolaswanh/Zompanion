using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set;}

    [Header("Animator Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    [Header("Movement")]
    [SerializeField] [Range(1f, 3f)] float moveSpeed = 1f;
    [SerializeField] private float deadZone = 0.01f;

    private Vector2 moveInput;
    private Animator animator;
    private Rigidbody2D rb;
    private InputSystem_Actions playerInput;
    private Vector2 lastDir = Vector2.down;
    private bool isMoving;


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
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        EnableMove();

        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        playerInput.Player.Move.performed -= OnMove;
        playerInput.Player.Move.canceled -= OnMove;

        DisableMove();
    }


    void Start()
    {
        
    }

    void Update()
    {
        isMoving = moveInput.sqrMagnitude > deadZone * deadZone;

        lastDir = LastDir();

        // 动作
        animator.SetFloat(moveXParam, lastDir.x);
        animator.SetFloat(moveYParam, lastDir.y);
        animator.SetFloat(speedParam, isMoving ? 1f : 0f);

        // // 位移
        // if (isMoving)
        // {
        //     Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f).normalized * (moveSpeed * Time.deltaTime);
        //     transform.position += delta;
        // }
    }

    void FixedUpdate()
    {
        Vector2 dir = moveInput.normalized;
        Vector2 targetPos = rb.position + dir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public Vector2 LastDir()
    {
        // 决定移动方向和最后面向方向
        Vector2 dir = lastDir;
        if (isMoving)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                dir = new Vector2(Mathf.Sign(moveInput.x), 0);
            else
                dir = new Vector2(0, Mathf.Sign(moveInput.y));
        }

        lastDir = dir;
        return dir;
    }

    public void DisableMove()
    {
        playerInput.Disable();
    }

    public void EnableMove()
    {
        playerInput.Enable();
    }
}
