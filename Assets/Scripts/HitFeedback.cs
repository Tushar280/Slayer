using System.Collections;
using UnityEngine;

/// <summary>
/// Flashes the dragon red briefly when it takes damage.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] meshRenderers;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private Material[][] spawnedMaterials;
    private Color[][] originalColors;
    private bool isFlashing;

    private void Awake()
    {
        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        // Cache the unique materials so the flash colours don't permanently alter the shared assets
        if (meshRenderers.Length == 0) return;

        spawnedMaterials = new Material[meshRenderers.Length][];
        originalColors = new Color[meshRenderers.Length][];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null)
            {
                spawnedMaterials[i] = null;
                originalColors[i] = null;
                continue;
            }

            var shared = meshRenderers[i].sharedMaterials;
            var instances = new Material[shared.Length];
            var originals = new Color[shared.Length];
            for (int j = 0; j < shared.Length; j++)
            {
                instances[j] = new Material(shared[j]);
                originals[j] = GetColor(instances[j]);
            }
            spawnedMaterials[i] = instances;
            originalColors[i] = originals;
            meshRenderers[i].materials = instances;
        }
    }

    public void PlayHitFeedback()
    {
        if (isFlashing || spawnedMaterials == null) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;

        for (int i = 0; i < spawnedMaterials.Length; i++)
        {
            var materials = spawnedMaterials[i];
            if (materials == null) continue;
            foreach (var material in materials) SetColor(material, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < spawnedMaterials.Length; i++)
        {
            var materials = spawnedMaterials[i];
            if (materials == null) continue;
            var originals = originalColors[i];
            for (int j = 0; j < materials.Length; j++)
            {
                SetColor(materials[j], originals != null && j < originals.Length ? originals[j] : Color.white);
            }
        }

        isFlashing = false;
    }

    private static Color GetColor(Material material)
    {
        if (material == null) return Color.white;
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    private static void SetColor(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }
}