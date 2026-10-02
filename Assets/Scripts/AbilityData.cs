using UnityEngine;

/// <summary>
/// Data describing a single dragon ability: damage, range, cooldown and animation.
/// </summary>
[System.Serializable]
public class AbilityData
{
    public string abilityName;
    public int damage;
    public float cooldown;
    public float range;
    public string animatorTrigger;
    public GameObject vfxPrefab; // Optional fire particle / hit effect

    [HideInInspector] public float nextReadyTime;

    public bool IsReady => Time.time >= nextReadyTime;

    public void TriggerCooldown()
    {
        nextReadyTime = Time.time + cooldown;
    }

    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    public float CooldownProgress => cooldown > 0 ? Mathf.Clamp01(CooldownRemaining / cooldown) : 0f;
}