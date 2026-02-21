using UnityEngine;

public class ZombieMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] [Range(1f, 3f)] float moveSpeed = 1f;

    [Header("Animator Params")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        
    }
}
