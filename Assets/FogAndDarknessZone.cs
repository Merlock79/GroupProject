using UnityEngine;

public class FogAndDarknessZone : MonoBehaviour
{
    [Header("Zone Boundaries")]
    [Tooltip("Radius (in meters) from the center of this object where the fog and darkness are at full strength.")]
    public float radius = 10f;

    [Tooltip("Distance (in meters) beyond the radius where the effect smoothly fades out to zero.")]
    public float fadeDistance = 10f;

    [Header("Visual Settings")]
    [Tooltip("How dark the screen gets inside this zone (0 = normal, 1 = pitch black overlay).")]
    [Range(0f, 1f)]
    public float screenDarkness = 0.8f;

    [Tooltip("Enable fog adjustments inside this zone?")]
    public bool enableFog = true;

    [Tooltip("How far you can see inside this zone (Fog End Distance). Lower values make it harder to see far.")]
    public float visibilityDistance = 15f;

    [Tooltip("Color of the fog and screen darkness tint inside this zone.")]
    public Color zoneColor = new Color(0.05f, 0.05f, 0.05f);

    [Header("Blending Priority")]
    [Tooltip("Priority used when multiple zones overlap. Higher priority zones override lower priority ones.")]
    public int priority = 0;

    // Calculates the influence of this zone on the player (0 = no influence, 1 = full influence)
    public float GetInfluence(Vector3 playerPosition, out float darkness, out float visibility, out Color color)
    {
        darkness = 0f;
        visibility = 1000f; // default high visibility
        color = zoneColor;

        float distance = Vector3.Distance(playerPosition, transform.position);
        float maxRange = radius + fadeDistance;

        if (distance > maxRange)
        {
            return 0f;
        }

        float influence = 1f;
        if (distance > radius)
        {
            // Linear fade out between radius and maxRange
            float t = (distance - radius) / Mathf.Max(0.001f, fadeDistance);
            influence = 1f - Mathf.Clamp01(t);
        }

        darkness = screenDarkness * influence;
        visibility = visibilityDistance; // We pass the raw visibility; the controller will blend it based on influence
        
        return influence;
    }

    private void OnDrawGizmos()
    {
        // Draw spheres in Editor Scene View to visualize the zone
        // Inner sphere: Full effect
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);

        // Outer sphere: Fade boundary
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radius + fadeDistance);

        // Center marker
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}