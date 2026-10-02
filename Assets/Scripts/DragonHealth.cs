using UnityEngine;

/// <summary>
/// Shared health logic for both dragons: tracks HP, keeps the health bar in sync,
/// plays the hit / death animations and flashes the mesh on damage.
/// </summary>
public abstract class DragonHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [SerializeField] private Healthbar healthBar;

    [Tooltip("Minimum time between two registered hits. Prevents a single attack from being counted multiple times.")]
    [SerializeField] private float hitInterval = 0.5f;

    private Animator cachedAnimator;
    private float nextHitTimeAllowed;

    public bool IsAlive => currentHealth > 0;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;

        // Ignore stray extra hits that land right after a real one -
        // one attack should flash and stagger the dragon exactly once.
        if (Time.time < nextHitTimeAllowed) return;
        nextHitTimeAllowed = Time.time + hitInterval;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (healthBar != null) healthBar.SetHealth(currentHealth);

        if (!IsAlive)
        {
            TrySetTrigger("Die");
        }
        else
        {
            TrySetTrigger("GetHit");
        }

        var feedback = GetComponent<HitFeedback>();
        if (feedback != null) feedback.PlayHitFeedback();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);
    }

    private void TrySetTrigger(string triggerName)
    {
        var animator = GetAnimator();
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

    private Animator GetAnimator()
    {
        if (cachedAnimator == null) cachedAnimator = GetComponentInChildren<Animator>();
        return cachedAnimator;
    }
}