using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Animator Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    [Header("Movement")]
    [SerializeField] [Range(1f, 3f)] private float moveSpeed = 1f;
    [SerializeField] private float deadZone = 0.01f;
    [SerializeField] private Transform sortPivot;

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
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var ySort = GetComponent<YSortRenderer>();
        if (ySort != null && sortPivot != null)
            ySort.SetSortPivot(sortPivot);
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

    private void Update()
    {
        isMoving = moveInput.sqrMagnitude > deadZone * deadZone;

        // Keep facing direction synced with the last non-zero input.
        if (isMoving)
            lastDir = ToCardinalDirection(moveInput);

        if (animator != null)
        {
            animator.SetFloat(moveXParam, lastDir.x);
            animator.SetFloat(moveYParam, lastDir.y);
            animator.SetFloat(speedParam, isMoving ? 1f : 0f);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 dir = isMoving ? moveInput.normalized : Vector2.zero;
        Vector2 targetPos = rb.position + dir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > deadZone * deadZone)
            lastDir = ToCardinalDirection(moveInput);
    }

    public Vector2 LastDir()
    {
        return lastDir;
    }

    private Vector2 ToCardinalDirection(Vector2 input)
    {
        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);

        if (ax > ay)
            return new Vector2(Mathf.Sign(input.x), 0f);

        if (ay > ax)
            return new Vector2(0f, Mathf.Sign(input.y));

        // Avoid flicker when both axes are equal.
        if (Mathf.Abs(lastDir.x) > 0.5f)
            return new Vector2(Mathf.Sign(input.x), 0f);

        return new Vector2(0f, Mathf.Sign(input.y));
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
