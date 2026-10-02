using UnityEngine;

/// <summary>
/// Feeds the movement state into the player's Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private const string WalkingParameter = "IsWalking";

    [SerializeField] private PlayerMovement player;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        if (animator == null || player == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == WalkingParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(WalkingParameter, player.IsWalking());
                return;
            }
        }
    }
}