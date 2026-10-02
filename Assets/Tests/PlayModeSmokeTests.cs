using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Headless smoke tests for the battle scene. These run the real game loop so we can
/// prove the boss never shoves the player, cooldowns tick, and damage lands.
/// </summary>
public class PlayModeSmokeTests
{
    private const string GameScene = "Main Scene";
    private const float PushTolerance = 0.35f; // Metres the player may drift without input

    [UnitySetUp]
    public IEnumerator LoadBattleScene()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator BothDragonsStartAtFullHealthWithSyncedBars()
    {
        yield return null;

        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var enemy = Object.FindAnyObjectByType<EnemyHealth>();

        Assert.IsNotNull(player, "No PlayerHealth in the battle scene");
        Assert.IsNotNull(enemy, "No EnemyHealth in the battle scene");

        Assert.AreEqual(1000, player.maxHealth, "Player max health");
        Assert.AreEqual(1000, enemy.maxHealth, "Enemy max health");
        Assert.AreEqual(1000, player.currentHealth, "Player current health");
        Assert.AreEqual(1000, enemy.currentHealth, "Enemy current health");

        AssertHealthbarSynced(player, "Player");
        AssertHealthbarSynced(enemy, "Enemy");

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator BossNeverPushesThePlayer()
    {
        yield return null;

        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var enemy = Object.FindAnyObjectByType<EnemyHealth>();

        var playerController = player.GetComponent<CharacterController>();
        Assert.IsNotNull(playerController, "Player needs a CharacterController");
        Assert.IsNull(enemy.GetComponent<CharacterController>(),
            "The boss must not have a CharacterController - two of them shove each other around");

        Vector3 startPosition = player.transform.position;
        float closestApproach = float.MaxValue;

        // 10 seconds of simulated fighting with no player input
        float elapsed = 0f;
        while (elapsed < 10f)
        {
            elapsed += Time.deltaTime;

            Vector3 offset = enemy.transform.position - player.transform.position;
            offset.y = 0f;
            closestApproach = Mathf.Min(closestApproach, offset.magnitude);

            yield return null;
        }

        float drift = Vector3.Distance(
            new Vector3(startPosition.x, 0f, startPosition.z),
            new Vector3(player.transform.position.x, 0f, player.transform.position.z));

        Assert.Less(drift, PushTolerance,
            $"The player was shoved {drift:F3}m by the boss (closest approach was {closestApproach:F2}m)");
        Assert.Less(closestApproach, 8f, "The boss never closed in at all");

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator CooldownsTickAndBlockRecast()
    {
        yield return null;

        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var enemy = Object.FindAnyObjectByType<EnemyHealth>();
        var combat = player.GetComponent<DragonCombat>();

        Assert.IsTrue(combat.fireAttack.IsReady, "Fire attack should start off cooldown");

        combat.CastAbility(combat.fireAttack, enemy.gameObject);

        Assert.IsFalse(combat.fireAttack.IsReady, "Fire attack should be on cooldown immediately after casting");
        Assert.Greater(combat.fireAttack.CooldownRemaining, 0f, "Cooldown timer did not start");

        // A second cast while cooling down must be rejected
        Assert.IsFalse(combat.CanCast(combat.fireAttack, 1f), "CanCast ignored the running cooldown");

        float startRemaining = combat.fireAttack.CooldownRemaining;
        yield return new WaitForSeconds(1f);
        float afterOneSecond = combat.fireAttack.CooldownRemaining;

        Assert.Less(afterOneSecond, startRemaining, "Cooldown timer is not counting down");
        Assert.GreaterOrEqual(afterOneSecond, startRemaining - 1.5f, "Cooldown timer dropped far too fast");

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator AttacksDealDamageAndUpdateTheHealthBar()
    {
        yield return null;

        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var enemy = Object.FindAnyObjectByType<EnemyHealth>();
        var combat = player.GetComponent<DragonCombat>();

        // Bring the boss into striking distance
        enemy.transform.position = new Vector3(4f, 0f, 0f);
        yield return null;

        int before = enemy.currentHealth;

        combat.CastAbility(combat.flyAttack, enemy.gameObject);

        // Wait past the impact delay so the damage frame lands
        yield return new WaitForSeconds(0.8f);

        Assert.Less(enemy.currentHealth, before, "The attack did no damage");

        AssertHealthbarSynced(enemy, "Enemy after taking a hit");

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator HitboxRespondsToAnimationEvents()
    {
        yield return null;

        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var hitbox = player.GetComponentInChildren<AnimationHitbox>(includeInactive: true);

        Assert.IsNotNull(hitbox, "Player has no AnimationHitbox - the attack clip events would be ignored");

        var collider = player.transform.Find("AttackHitbox");
        Assert.IsNotNull(collider, "Player has no AttackHitbox child");

        // These are the method names baked into the dragon attack clips
        var type = hitbox.GetType();
        Assert.IsNotNull(type.GetMethod("EnableHitboxEvent"), "EnableHitboxEvent receiver is missing");
        Assert.IsNotNull(type.GetMethod("DisableHitboxEvent"), "DisableHitboxEvent receiver is missing");

        var box = collider.GetComponent<Collider>();
        Assert.IsFalse(box.enabled, "The hitbox should start disabled");

        type.GetMethod("EnableHitboxEvent").Invoke(hitbox, null);
        yield return null;
        Assert.IsTrue(box.enabled, "EnableHitboxEvent did not enable the hitbox");

        type.GetMethod("DisableHitboxEvent").Invoke(hitbox, null);
        yield return null;
        Assert.IsFalse(box.enabled, "DisableHitboxEvent did not disable the hitbox");

        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator VictoryPanelAppearsWhenTheBossDies()
    {
        yield return null;

        var enemy = Object.FindAnyObjectByType<EnemyHealth>();
        var manager = Object.FindAnyObjectByType<GameManager>();

        enemy.TakeDamage(5000);

        yield return null;

        Assert.IsTrue(manager.IsGameOver, "Game over was not triggered");
        Assert.Less(Time.timeScale, 1f, "Slow motion should kick in on game over");

        LogAssert.NoUnexpectedReceived();
    }

    private static void AssertHealthbarSynced(DragonHealth health, string label)
    {
        var bar = ReadPrivateField<Healthbar>(health, "healthBar");
        Assert.IsNotNull(bar, label + " health bar is not assigned");

        var slider = ReadPrivateField<UnityEngine.UI.Slider>(bar, "slider");
        Assert.IsNotNull(slider, label + " health bar has no slider");

        Assert.AreEqual(health.currentHealth, Mathf.RoundToInt(slider.value),
            label + " health bar does not match current health");
    }

    private static T ReadPrivateField<T>(object target, string fieldName) where T : class
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.DeclaredOnly;

        // Private fields can live on a base class, so walk the hierarchy explicitly
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(fieldName, flags);
            if (field == null) continue;

            return field.GetValue(target) as T;
        }

        return null;
    }
}
