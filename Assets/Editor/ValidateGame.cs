#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static validation of the game scene wiring: reports missing scripts, broken
/// references and ability triggers that don't exist on the assigned animators.
/// </summary>
public static class ValidateGame
{
    [MenuItem("Tools/Validate Game")]
    public static void Run()
    {
        var report = new StringBuilder();
        int problems = 0;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity", OpenSceneMode.Single);

        // --- Missing scripts -------------------------------------------------
        var missing = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in missing)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                {
                    report.AppendLine($"MISSING SCRIPT on '{go.name}'");
                    problems++;
                }
            }
        }

        // --- Core references -------------------------------------------------
        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var enemy = Object.FindAnyObjectByType<EnemyHealth>();
        var gm = Object.FindAnyObjectByType<GameManager>();
        var pause = Object.FindAnyObjectByType<PauseMenu>();

        problems += Check(report, "PlayerHealth present", player != null);
        problems += Check(report, "EnemyHealth present", enemy != null);
        problems += Check(report, "GameManager present", gm != null);
        problems += Check(report, "PauseMenu present", pause != null);
        problems += Check(report, "Player Healthbar wired", player != null && HasHealthbar(player));
        problems += Check(report, "Enemy Healthbar wired", enemy != null && HasHealthbar(enemy));
        problems += Check(report, "Player has CharacterController", player != null && player.GetComponent<CharacterController>() != null);
        problems += Check(report, "Player has HitFeedback", player != null && player.GetComponent<HitFeedback>() != null);
        problems += Check(report, "Enemy has HitFeedback", enemy != null && enemy.GetComponent<HitFeedback>() != null);
        problems += Check(report, "Enemy has AI", enemy != null && enemy.GetComponent<EnemyDragonAI>() != null);
        problems += Check(report, "Player has movement", player != null && player.GetComponent<PlayerMovement>() != null);
        problems += Check(report, "Player has combat controller", player != null && player.GetComponent<PlayerCombatController>() != null);

        // A second CharacterController would shove the player around, so the boss must not have one
        problems += Check(report, "Enemy has NO CharacterController (no pushing)",
            enemy != null && enemy.GetComponent<CharacterController>() == null);

        // --- Ability HUD ------------------------------------------------------
        var abilityBar = Object.FindAnyObjectByType<AbilityBar>();
        problems += Check(report, "AbilityBar present", abilityBar != null);
        if (abilityBar != null)
        {
            var barSo = new SerializedObject(abilityBar);
            problems += Check(report, "HUD fire slot wired", HasRef(barSo, "fireSlot"));
            problems += Check(report, "HUD tail slot wired", HasRef(barSo, "tailSlot"));
            problems += Check(report, "HUD fly slot wired", HasRef(barSo, "flySlot"));
            problems += Check(report, "HUD fire icon assigned", HasRef(barSo, "fireIcon"));
            problems += Check(report, "HUD tail icon assigned", HasRef(barSo, "tailIcon"));
            problems += Check(report, "HUD fly icon assigned", HasRef(barSo, "flyIcon"));
            problems += Check(report, "HUD owner is the player's DragonCombat", HasRef(barSo, "owner"));
        }

        // Every slot must actually show something, not just exist
        foreach (var slot in Object.FindObjectsByType<AbilitySlotUI>(FindObjectsInactive.Include))
        {
            problems += Check(report, $"Slot '{slot.name}' has an icon", SlotHasImage(slot, "Icon"));
            problems += Check(report, $"Slot '{slot.name}' has a cooldown sweep", SlotHasImage(slot, "CooldownOverlay"));
            problems += Check(report, $"Slot '{slot.name}' shows a timer", SlotHasText(slot, "CooldownText"));
            problems += Check(report, $"Slot '{slot.name}' shows a key", SlotHasText(slot, "KeyLabel"));
        }

        // --- Animators / ability triggers -------------------------------------
        if (player != null) problems += ValidateAnimator(report, "Player", player.GetComponentInChildren<Animator>());
        if (enemy != null) problems += ValidateAnimator(report, "Enemy", enemy.GetComponentInChildren<Animator>());

        if (player != null) problems += ValidateAbilities(report, "Player", player.GetComponent<DragonCombat>());
        if (enemy != null) problems += ValidateAbilities(report, "Enemy", enemy.GetComponent<DragonCombat>());

        // --- Pause / escape --------------------------------------------------
        if (pause != null)
        {
            var so = new SerializedObject(pause);
            var panel = so.FindProperty("pausePanel");
            bool ok = panel != null && panel.objectReferenceValue != null;
            report.AppendLine($"Pause panel assigned: {ok}");
            if (!ok) problems++;
        }

        report.AppendLine();
        report.AppendLine(problems == 0 ? "VALIDATION PASSED - no problems found" : $"VALIDATION FOUND {problems} PROBLEM(S)");

        Debug.Log(report.ToString());
        EditorSceneManager.SaveScene(scene);
        EditorApplication.Exit(problems == 0 ? 0 : 1);
    }

    private static bool HasHealthbar(DragonHealth health)
    {
        var so = new SerializedObject(health);
        var bar = so.FindProperty("healthBar");
        return bar != null && bar.objectReferenceValue != null;
    }

    private static bool HasRef(SerializedObject so, string path)
    {
        var property = so.FindProperty(path);
        return property != null && property.objectReferenceValue != null;
    }

    private static bool SlotHasImage(AbilitySlotUI slot, string childName)
    {
        return slot.GetComponentInChildren<UnityEngine.UI.Image>(includeInactive: true) != null
               && slot.transform.Find(childName) != null;
    }

    private static bool SlotHasText(AbilitySlotUI slot, string childName)
    {
        return slot.transform.Find(childName) != null
               && slot.transform.Find(childName).GetComponent<UnityEngine.UI.Text>() != null;
    }

    private static int ValidateAnimator(StringBuilder report, string label, Animator animator)
    {
        if (animator == null)
        {
            report.AppendLine($"{label}: NO ANIMATOR");
            return 1;
        }

        if (animator.runtimeAnimatorController == null)
        {
            report.AppendLine($"{label}: animator has NO CONTROLLER");
            return 1;
        }

        report.AppendLine($"{label}: controller '{animator.runtimeAnimatorController.name}'");
        return 0;
    }

    private static int ValidateAbilities(StringBuilder report, string label, DragonCombat combat)
    {
        if (combat == null)
        {
            report.AppendLine($"{label}: NO DragonCombat");
            return 1;
        }

        var animator = combat.GetComponentInChildren<Animator>();
        if (animator == null) return 0;

        var abilities = new[] { combat.fireAttack, combat.tailAttack, combat.flyAttack };
        int problems = 0;

        foreach (var ability in abilities)
        {
            if (ability == null) continue;

            bool found = false;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == ability.animatorTrigger && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                report.AppendLine($"{label}: ability '{ability.abilityName}' trigger '{ability.animatorTrigger}' MISSING on animator");
                problems++;
            }
        }

        return problems;
    }

    private static int Check(StringBuilder report, string label, bool ok)
    {
        report.AppendLine($"{label}: {(ok ? "OK" : "FAIL")}");
        return ok ? 0 : 1;
    }
}
#endif