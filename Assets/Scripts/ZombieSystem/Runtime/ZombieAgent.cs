using UnityEngine;

public class ZombieAgent : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform sortPivot;
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string faceDirectionParam = "FaceDirection";
    [SerializeField] private bool useCardinalDirection = true;
    [SerializeField] [Min(0.00001f)] private float movementEpsilon = 0.0001f;

    public int InstanceId { get; private set; }
    public ZombieDefinitionSO Definition { get; private set; }

    private Vector2 _lastDir = Vector2.down;

    public void Initialize(int instanceId, ZombieDefinitionSO definition)
    {
        InstanceId = instanceId;
        Definition = definition;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        var ySort = GetComponent<YSortRenderer>();
        if (ySort != null && sortPivot != null)
            ySort.SetSortPivot(sortPivot);
    }

    public void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, speed * Time.deltaTime);
        Vector3 delta = next - current;
        transform.position = next;

        bool isMoving = delta.sqrMagnitude > movementEpsilon;
        UpdateAnimatorByDelta(delta, isMoving);
    }

    public void SetIdle()
    {
        UpdateAnimatorByDelta(Vector3.zero, false);
    }

    private void UpdateAnimatorByDelta(Vector3 delta, bool isMoving)
    {
        if (isMoving)
        {
            Vector2 dir = new Vector2(delta.x, delta.y).normalized;
            _lastDir = useCardinalDirection ? ToCardinalDirection(dir) : dir;
        }

        if (animator == null) return;

        animator.SetFloat(moveXParam, _lastDir.x);
        animator.SetFloat(moveYParam, _lastDir.y);
        animator.SetFloat(speedParam, isMoving ? 1f : 0f);
        animator.SetFloat(faceDirectionParam, DirToFace(_lastDir));
    }

    private static Vector2 ToCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);

        return new Vector2(0f, Mathf.Sign(input.y));
    }

    private static float DirToFace(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > 0.01f)
            return dir.y > 0f ? 1f : 0f; // Up / Down
        return dir.x < 0f ? 2f : 3f;     // Left / Right
    }

}
