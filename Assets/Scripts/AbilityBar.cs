using UnityEngine;

/// <summary>
/// Drives the ability HUD: configures each slot from the player's abilities and
/// refreshes the cooldown sweep and countdown every frame.
/// </summary>
public class AbilityBar : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private AbilitySlotUI fireSlot;
    [SerializeField] private AbilitySlotUI tailSlot;
    [SerializeField] private AbilitySlotUI flySlot;

    [Header("Icons")]
    [SerializeField] private Sprite fireIcon;
    [SerializeField] private Sprite tailIcon;
    [SerializeField] private Sprite flyIcon;

    [Header("Key Hints")]
    [SerializeField] private string fireKey = "1";
    [SerializeField] private string tailKey = "2";
    [SerializeField] private string flyKey = "3";

    [Header("Owner")]
    [SerializeField] private DragonCombat owner;

    private void Start()
    {
        if (owner == null)
        {
            var controller = FindAnyObjectByType<PlayerCombatController>();
            if (controller != null) owner = controller.GetComponent<DragonCombat>();
        }

        if (fireSlot != null) fireSlot.Configure(fireIcon, fireKey);
        if (tailSlot != null) tailSlot.Configure(tailIcon, tailKey);
        if (flySlot != null) flySlot.Configure(flyIcon, flyKey);
    }

    private void Update()
    {
        if (owner == null) return;

        if (fireSlot != null) fireSlot.UpdateCooldown(owner.fireAttack);
        if (tailSlot != null) tailSlot.UpdateCooldown(owner.tailAttack);
        if (flySlot != null) flySlot.UpdateCooldown(owner.flyAttack);
    }
}
