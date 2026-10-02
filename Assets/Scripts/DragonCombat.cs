using System.Collections;
using UnityEngine;

/// <summary>
/// Executes dragon abilities: plays the animation, spawns VFX and applies damage.
/// </summary>
public class DragonCombat : MonoBehaviour
{
    // Triggers match the parameters exposed by the dragon animator controllers.
    [Header("Abilities")]
    public AbilityData fireAttack = new AbilityData { abilityName = "Fire Attack", damage = 60, cooldown = 4f, range = 12f, animatorTrigger = "ClawAttack" };
    public AbilityData tailAttack = new AbilityData { abilityName = "Tail Attack", damage = 35, cooldown = 2.5f, range = 6f, animatorTrigger = "BasicAttack" };
    public AbilityData flyAttack = new AbilityData { abilityName = "Fly Attack", damage = 90, cooldown = 8f, range = 9f, animatorTrigger = "HornAttack" };

    [Header("Damage Multiplier")]
    [Tooltip("Scales all outgoing damage. Used to tune how threatening each dragon feels.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("References")]
    [SerializeField] private Transform attackOrigin; // Point in front of the dragon
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationHitbox hitbox; // Optional - deals damage on contact during the swing

    [Header("Timing")]
    [SerializeField] private float impactDelay = 0.4f; // Time from animation start to the damage frame
    [SerializeField] private float recoveryDelay = 0.6f; // Time spent finishing the animation

    /// <summary>Grace distance so hits that land slightly out of range still connect.</summary>
    private const float RangeGrace = 1.5f;

    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (attackOrigin == null) attackOrigin = transform;
        if (hitbox == null) hitbox = GetComponentInChildren<AnimationHitbox>(includeInactive: true);
    }

    /// <summary>Can this ability be used right now against a target at the given distance?</summary>
    public bool CanCast(AbilityData ability, float distanceToTarget)
    {
        if (ability == null || isAttacking || Time.timeScale <= 0f) return false;
        if (!ability.IsReady) return false;
        return distanceToTarget <= ability.range;
    }

    public void CastAbility(AbilityData ability, GameObject target)
    {
        if (ability == null || isAttacking || !ability.IsReady) return;

        ability.TriggerCooldown();
        StartCoroutine(ExecuteAbilityRoutine(ability, target));
    }

    private IEnumerator ExecuteAbilityRoutine(AbilityData ability, GameObject target)
    {
        isAttacking = true;

        FaceTarget(target);
        TrySetTrigger(ability.animatorTrigger);

        int outgoingDamage = Mathf.RoundToInt(ability.damage * damageMultiplier);

        // A hitbox deals the damage itself when the dragon actually connects
        if (hitbox != null)
        {
            hitbox.BeginSwing(outgoingDamage);
        }

        if (ability.vfxPrefab != null)
        {
            var vfx = Instantiate(ability.vfxPrefab, attackOrigin.position, attackOrigin.rotation);
            Destroy(vfx, 2.5f);
        }

        yield return new WaitForSeconds(impactDelay);

        // Fallback for dragons without a hitbox
        if (hitbox == null) ApplyDamage(ability, target, outgoingDamage);

        yield return new WaitForSeconds(recoveryDelay);
        isAttacking = false;
    }

    private void FaceTarget(GameObject target)
    {
        if (target == null) return;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ApplyDamage(AbilityData ability, GameObject target, int outgoingDamage)
    {
        if (target == null || ability == null) return;

        // Allow a small margin so hits that were slightly out of range still connect
        if (Vector3.Distance(transform.position, target.transform.position) > ability.range + RangeGrace) return;

        var targetHealth = target.GetComponentInParent<DragonHealth>();
        if (targetHealth == null || !targetHealth.IsAlive) return;

        targetHealth.TakeDamage(outgoingDamage);
    }

    private void TrySetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }
}