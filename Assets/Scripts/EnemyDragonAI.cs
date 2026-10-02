using UnityEngine;

/// <summary>
/// Drives the AI dragon: chases the player, holds at its preferred range and picks attacks.
/// Movement is done with the transform (no physics) so the enemy can never push the player.
/// </summary>
[RequireComponent(typeof(DragonCombat))]
public class EnemyDragonAI : MonoBehaviour
{
    public enum AIState { Idle, Chase, Attack, Recover }

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 6f;   // Where the enemy settles and attacks from
    [SerializeField] private float minDistance = 4.5f;  // Hard floor - the enemy never comes closer than this
    [SerializeField] private float rotationSpeed = 0.08f;

    [Header("Difficulty")]
    [SerializeField] private float decisionInterval = 1.6f; // Pause between attack decisions
    [SerializeField] private float recoverDuration = 0.9f;   // Idle pause after each attack

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float groundOffset = 0f;

    private const string WalkingParameter = "IsWalking";

    private DragonCombat combat;
    private AIState currentState = AIState.Idle;
    private bool hasWalkingParameter;
    private float nextDecisionTime;
    private float groundY;
    private bool groundYCaptured;

    private void Awake()
    {
        combat = GetComponent<DragonCombat>();
        var animator = GetComponentInChildren<Animator>();
        hasWalkingParameter = HasParameter(animator, WalkingParameter, AnimatorControllerParameterType.Bool);

        if (playerTransform == null)
        {
            var player = FindAnyObjectByType<PlayerHealth>();
            if (player != null) playerTransform = player.transform;
        }
    }

    private void Start()
    {
        groundY = transform.position.y;
        groundYCaptured = true;
    }

    private void Update()
    {
        // Don't act while paused or after the battle has ended
        if (Time.timeScale <= 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (playerTransform == null || combat == null) return;

        // Freeze while an attack animation is playing
        if (combat.IsAttacking)
        {
            SetWalking(false);
            return;
        }

        float distance = HorizontalDistanceToPlayer();
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        switch (currentState)
        {
            case AIState.Idle:
                currentState = distance > stopDistance ? AIState.Chase : AIState.Attack;
                break;

            case AIState.Chase:
                if (distance <= stopDistance)
                {
                    SetWalking(false);
                    currentState = AIState.Attack;
                    nextDecisionTime = Time.time + decisionInterval * 0.5f;
                }
                else if (distance <= minDistance)
                {
                    // In the no-go zone: back off instead of shoving the player
                    Retreat(toPlayer, distance);
                }
                else
                {
                    MoveTowards(toPlayer);

                    // Only consider a ranged attack while closing the gap
                    if (Time.time >= nextDecisionTime && TryCast(combat.fireAttack, distance))
                    {
                        currentState = AIState.Recover;
                        nextDecisionTime = Time.time + recoverDuration;
                    }
                }
                break;

            case AIState.Attack:
                SetWalking(false);
                // Keep the player at arm's length while attacking
                if (distance < minDistance) Retreat(toPlayer, distance);

                if (Time.time >= nextDecisionTime)
                {
                    if (TryCastBestAbility(distance))
                    {
                        currentState = AIState.Recover;
                        nextDecisionTime = Time.time + recoverDuration;
                    }
                    else
                    {
                        nextDecisionTime = Time.time + 0.25f;
                    }
                }
                break;

            case AIState.Recover:
                SetWalking(false);
                if (Time.time >= nextDecisionTime)
                {
                    currentState = distance > stopDistance ? AIState.Chase : AIState.Attack;
                    nextDecisionTime = Time.time + decisionInterval;
                }
                break;
        }
    }

    private void MoveTowards(Vector3 toPlayer)
    {
        Vector3 direction = toPlayer.normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), rotationSpeed);
        transform.position += direction * moveSpeed * Time.deltaTime;
        StayGrounded();
        SetWalking(true);
    }

    private void Retreat(Vector3 toPlayer, float distance)
    {
        if (distance < 0.01f) return;

        Vector3 away = -toPlayer.normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(away, Vector3.up), rotationSpeed);
        transform.position += away * moveSpeed * 0.6f * Time.deltaTime;
        StayGrounded();
        SetWalking(true);
    }

    /// <summary>Attempts to cast, preferring the heaviest hitter first.</summary>
    private bool TryCastBestAbility(float distance)
    {
        return TryCast(combat.flyAttack, distance)
               || TryCast(combat.tailAttack, distance)
               || TryCast(combat.fireAttack, distance);
    }

    private bool TryCast(AbilityData ability, float distance)
    {
        if (!combat.CanCast(ability, distance)) return false;

        combat.CastAbility(ability, playerTransform.gameObject);
        return true;
    }

    private float HorizontalDistanceToPlayer()
    {
        Vector3 difference = playerTransform.position - transform.position;
        difference.y = 0f;
        return difference.magnitude;
    }

    private void StayGrounded()
    {
        if (!groundYCaptured) return;
        Vector3 position = transform.position;
        position.y = groundY + groundOffset;
        transform.position = position;
    }

    private void SetWalking(bool walking)
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && hasWalkingParameter) animator.SetBool(WalkingParameter, walking);
    }

    private static bool HasParameter(Animator target, string name, AnimatorControllerParameterType type)
    {
        if (target == null) return false;

        foreach (var parameter in target.parameters)
        {
            if (parameter.name == name && parameter.type == type) return true;
        }
        return false;
    }
}