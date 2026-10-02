using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thin wrapper around the slider used to display dragon health.
/// </summary>
public class Healthbar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
    }

    public void SetHealth(int health)
    {
        if (slider != null) slider.value = health;
    }

    public void SetMaxHealth(int health)
    {
        if (slider == null) return;
        slider.maxValue = health;
        slider.value = health;
    }
}