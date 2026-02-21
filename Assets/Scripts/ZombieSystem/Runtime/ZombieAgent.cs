using UnityEngine;

public class ZombieAgent : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    public int InstanceId { get; private set; }
    public ZombieDefinitionSO Definition { get; private set; }

    public void Initialize(int instanceId, ZombieDefinitionSO definition)
    {
        InstanceId = instanceId;
        Definition = definition;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void MoveTowards(Vector3 targetPosition, float speed)
    {
        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, speed * Time.deltaTime);
        Vector3 delta = next - current;
        transform.position = next;

        if (animator == null) return;

        if (delta.sqrMagnitude > 0.0001f)
        {
            Vector2 dir = delta.normalized;
            animator.SetFloat(moveXParam, dir.x);
            animator.SetFloat(moveYParam, dir.y);
            animator.SetFloat(speedParam, 1f);
        }
        else
        {
            animator.SetFloat(speedParam, 0f);
        }
    }
}

