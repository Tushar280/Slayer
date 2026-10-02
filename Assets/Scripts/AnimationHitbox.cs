using UnityEngine;

/// <summary>
/// Attack hitbox driven by the dragon attack animations.
/// The clips contain EnableHitboxEvent / DisableHitboxEvent animation events, so this
/// component turns a collider on for the length of the swing and damages whatever it overlaps.
///
/// Put this component on the same GameObject as the Animator - Unity only delivers
/// animation events to components sitting beside it.
/// </summary>
public class AnimationHitbox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider hitbox;
    [SerializeField] private DragonHealth owner;

    [Header("Damage")]
    [Tooltip("Damage dealt per swing. DragonCombat overwrites this when a cast starts.")]
    [SerializeField] private int damage = 25;

    [Tooltip("How long the hitbox stays live when the attack clip has no hitbox events.")]
    [SerializeField] private float fallbackWindow = 0.3f;

    [Tooltip("Safety cap so an interrupted animation can never leave the hitbox stuck on.")]
    [SerializeField] private float maxSwingLength = 1.2f;

    [Tooltip("Minimum gap between two hits from the same swing.")]
    [SerializeField] private float rehitDelay = 0.25f;

    private float nextHitTime;
    private float hardEndTime;
    private bool swingInProgress;
    private bool hasHitThisSwing;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<DragonHealth>();
        SetHitboxEnabled(false);
    }

    /// <summary>Called by DragonCombat right before the attack animation starts.</summary>
    public void BeginSwing(int swingDamage)
    {
        damage = Mathf.Max(1, swingDamage);
        nextHitTime = 0f;
        hasHitThisSwing = false;
        hardEndTime = Time.time + Mathf.Max(fallbackWindow, 0.1f) + maxSwingLength;
        swingInProgress = true;
        SetHitboxEnabled(true);
    }

    // --- Animation event receivers -------------------------------------
    // These names match the events baked into the dragon attack clips.

    /// <summary>Animation event: opens the hitbox at the start of the swing.</summary>
    public void EnableHitboxEvent()
    {
        SetHitboxEnabled(true);
    }

    /// <summary>Animation event: closes the hitbox at the end of the swing.</summary>
    public void DisableHitboxEvent()
    {
        SetHitboxEnabled(false);
    }

    private void Update()
    {
        if (hitbox == null || !hitbox.enabled) return;

        // Safety net: an interrupted animation can never leave the hitbox stuck on.
        // A hitbox opened purely by an animation event is left alone - the clip closes it.
        if (swingInProgress && Time.time > hardEndTime)
        {
            swingInProgress = false;
            SetHitboxEnabled(false);
            return;
        }

        // Physics.OverlapBox is used instead of relying on trigger callbacks so it works
        // whether the other dragon is a CharacterController or not.
        var bounds = hitbox.bounds;
        var hits = Physics.OverlapBox(bounds.center, bounds.extents, hitbox.transform.rotation);

        foreach (var other in hits)
        {
            if (TryDamage(other)) return;
        }
    }

    private bool TryDamage(Collider other)
    {
        if (other == null || hitbox == null || !hitbox.enabled) return false;
        if (other.transform.IsChildOf(transform)) return false;
        if (Time.time < nextHitTime) return false;
        if (hasHitThisSwing) return false; // one successful hit per swing, no exceptions

        var target = other.GetComponentInParent<DragonHealth>();
        if (target == null || target == owner || !target.IsAlive) return false;

        nextHitTime = Time.time + rehitDelay; // stop a lingering overlap from spamming damage
        hasHitThisSwing = true;
        target.TakeDamage(damage);
        return true;
    }

    private void SetHitboxEnabled(bool enabled)
    {
        if (hitbox != null) hitbox.enabled = enabled;
    }
}
