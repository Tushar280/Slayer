using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maps player input to dragon abilities.
/// </summary>
[RequireComponent(typeof(DragonCombat))]
public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private GameObject enemyTarget;

    private DragonCombat combat;

    private void Awake()
    {
        combat = GetComponent<DragonCombat>();
    }

    private void Update()
    {
        // Don't allow casting while paused or after game over
        if (Time.timeScale <= 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        GameObject target = ResolveTarget();
        if (target == null || combat == null) return;

        var keyboard = Keyboard.current;
        bool firePressed = (keyboard != null && (keyboard.digit1Key.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame))
                           || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool tailPressed = keyboard != null && (keyboard.digit2Key.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
        bool flyPressed = keyboard != null && (keyboard.digit3Key.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame);

        if (firePressed) TryCast(combat.fireAttack, target);
        if (tailPressed) TryCast(combat.tailAttack, target);
        if (flyPressed) TryCast(combat.flyAttack, target);
    }

    private GameObject ResolveTarget()
    {
        if (enemyTarget == null)
        {
            var enemy = FindAnyObjectByType<EnemyHealth>();
            if (enemy != null) enemyTarget = enemy.gameObject;
        }
        return enemyTarget;
    }

    private void TryCast(AbilityData ability, GameObject target)
    {
        if (!combat.CanCast(ability, Vector3.Distance(transform.position, target.transform.position))) return;
        combat.CastAbility(ability, target);
    }
}