using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD slot for a single ability: shows the icon, a radial cooldown sweep,
/// the remaining seconds and the key you press to cast it.
/// </summary>
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay; // Image type: Filled (Radial 360)
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text keyLabel;
    [SerializeField] private Image readyHighlight;

    private void Reset()
    {
        cooldownOverlay = transform.Find("CooldownOverlay")?.GetComponent<Image>();
        cooldownText = transform.Find("CooldownText")?.GetComponent<Text>();
        keyLabel = transform.Find("KeyLabel")?.GetComponent<Text>();
        icon = transform.Find("Icon")?.GetComponent<Image>();
    }

    /// <summary>Called once when the slot is created.</summary>
    public void Configure(Sprite abilitySprite, string keyText)
    {
        if (icon != null)
        {
            icon.sprite = abilitySprite;
            icon.enabled = abilitySprite != null;
        }

        if (keyLabel != null) keyLabel.text = keyText;
    }

    /// <summary>Refreshes the cooldown readout from an ability's live timer.</summary>
    public void UpdateCooldown(AbilityData ability)
    {
        if (ability == null) return;

        float remaining = ability.CooldownRemaining;
        Refresh(remaining > 0f, remaining > 0f ? ability.CooldownProgress : 0f, remaining);
    }

    private void Refresh(bool isCooling, float progress, float remainingSeconds)
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = isCooling ? progress : 0f;
            cooldownOverlay.enabled = isCooling;
        }

        if (readyHighlight != null) readyHighlight.enabled = !isCooling;

        if (cooldownText == null) return;

        cooldownText.gameObject.SetActive(isCooling);
        if (isCooling) cooldownText.text = remainingSeconds.ToString("F1");
    }
}
