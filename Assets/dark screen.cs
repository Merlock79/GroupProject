using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class ScreenDarkening : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Transform representing the player (position used to evaluate zones).")]
    public Transform player;

    [Header("Global Smoothing")]
    [Tooltip("How quickly vignette intensity follows targetDarkness.")]
    public float smoothSpeed = 5f;

    [Header("Vignette")]
    public float defaultVignetteIntensity = 0f; // default intensity outside any zone

    [Header("Zones")]
    [Tooltip("If empty, all DarkZone components found in the scene will be used.")]
    public DarkZone[] zones;

    private Volume volume;
    private Vignette vignette;
    private float targetDarkness = 0f;

    void Awake()
    {
        SetupVignette();

        if (player == null)
            Debug.LogWarning("ScreenDarkening: Player Transform is not assigned.", this);

        if (zones == null || zones.Length == 0)
            zones = FindObjectsOfType<DarkZone>();
    }

    void Update()
    {
        if (vignette == null || player == null)
            return;

        HandleDarkness();
        ApplyDarkness();
    }

    // ---------------------------------------------------------
    // AUTO‑CREATE VIGNETTE + VOLUME
    // ---------------------------------------------------------
    void SetupVignette()
    {
        // Ensure Volume exists
        volume = GetComponent<Volume>();
        if (volume == null)
            volume = gameObject.AddComponent<Volume>();

        volume.isGlobal = true;

        // Ensure profile exists
        if (volume.profile == null)
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Ensure vignette exists on profile
        if (!volume.profile.TryGet<Vignette>(out vignette))
        {
            vignette = volume.profile.Add<Vignette>();
        }

        if (vignette != null)
        {
            vignette.active = true;

            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.color.overrideState = true;

            vignette.intensity.value = defaultVignetteIntensity;
            vignette.smoothness.value = 0.8f;
            vignette.color.value = Color.black;
        }
        else
        {
            Debug.LogError("ScreenDarkening: Failed to create/find Vignette in VolumeProfile.", this);
        }
    }

    // ---------------------------------------------------------
    // DARKNESS LOGIC (supports multiple zones)
    // ---------------------------------------------------------
    void HandleDarkness()
    {
        float highestInfluence = defaultVignetteIntensity;

        if (zones != null && zones.Length > 0)
        {
            foreach (var zone in zones)
            {
                if (zone == null || !zone.enabled)
                    continue;

                float influence = zone.EvaluateInfluence(player);
                // Use max influence so overlapping zones pick the strongest darkness
                if (influence > highestInfluence)
                    highestInfluence = influence;
            }
        }

        targetDarkness = Mathf.Clamp01(highestInfluence);
    }

    // ---------------------------------------------------------
    // APPLY DARKNESS
    // ---------------------------------------------------------
    void ApplyDarkness()
    {
        if (vignette == null)
            return;

        vignette.intensity.value = Mathf.Lerp(
            vignette.intensity.value,
            targetDarkness,
            Time.deltaTime * Mathf.Max(0.0001f, smoothSpeed)
        );
    }

    // ---------------------------------------------------------
    // EXTERNAL CONTROL (INTERACT OBJECTS)
    // ---------------------------------------------------------
    public void DisableDarkness()
    {
        targetDarkness = defaultVignetteIntensity;
    }

    // Editor helper: refresh zone list
    public void RefreshZones()
    {
        zones = FindObjectsOfType<DarkZone>();
    }
}

[System.Serializable]
public class DarkZone : MonoBehaviour
{
    [Header("Zone Area")]
    [Tooltip("Radius from this object's position where the zone reaches full effect.")]
    public float radius = 5f;

    [Tooltip("Distance beyond 'radius' where the zone fades out to zero. Total fade distance = radius + fadeDistance.")]
    public float fadeDistance = 5f;

    [Header("Darkness Settings")]
    [Range(0f, 1f)]
    public float maxDarkness = 0.7f;

    [Tooltip("Optional: If any of these lights are within lightRemoveDistance, darkness will be removed for this zone.")]
    public Transform[] lightSources;

    [Tooltip("If a light in this zone is closer than this distance to the player, the zone becomes fully lit (no darkness).")]
    public float lightRemoveDistance = 3f;

    [Header("Optional")]
    [Tooltip("Priority used if you later want to weight blending differently. Higher = stronger when equal influence.")]
    public int priority = 0;

    // Evaluate the zone influence (0..1) for the provided player transform
    // The method returns a darkness value in range [0, maxDarkness]
    public float EvaluateInfluence(Transform player)
    {
        if (player == null)
            return 0f;

        float distanceToPlayer = Vector3.Distance(player.position, transform.position);

        // Outside combined range -> no influence
        float maxRange = radius + fadeDistance;
        if (distanceToPlayer > maxRange)
            return 0f;

        // If any light in this zone is close enough to the player, remove darkness
        if (lightSources != null && lightSources.Length > 0)
        {
            foreach (var light in lightSources)
            {
                if (light == null) continue;
                float d = Vector3.Distance(player.position, light.position);
                if (d <= lightRemoveDistance)
                    return 0f;
            }
        }

        // If inside radius => full darkness (maxDarkness)
        if (distanceToPlayer <= radius)
            return maxDarkness;

        // Between radius and maxRange -> fade from maxDarkness to 0
        float t = (distanceToPlayer - radius) / Mathf.Max(0.0001f, fadeDistance); // 0..1
        float influence = Mathf.Lerp(maxDarkness, 0f, Mathf.Clamp01(t));
        return influence;
    }

    // Draw zone gizmos for easier layout in the Editor
    void OnDrawGizmosSelected()
    {
        Color baseColor = new Color(0f, 0f, 0f, 0.25f);
        Gizmos.color = baseColor;
        Gizmos.DrawSphere(transform.position, radius);

        Color fadeColor = new Color(0f, 0f, 0f, 0.15f);
        Gizmos.color = fadeColor;
        Gizmos.DrawWireSphere(transform.position, radius + fadeDistance);

        // Draw light sources
        if (lightSources != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var l in lightSources)
            {
                if (l == null) continue;
                Gizmos.DrawLine(transform.position, l.position);
                Gizmos.DrawSphere(l.position, 0.1f);
            }
        }
    }
}