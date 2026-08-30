using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DarkScreenZones : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        [Tooltip("Exactly 4 empty Transforms in order (A → B → C → D) defining the square/quad on the XZ plane.")]
        public Transform[] corners = new Transform[4];

        [Range(0f, 1f)]
        [Tooltip("Screen darkening strength (0 = no dark, 1 = solid black).")]
        public float darkness = 0.6f;

        [Tooltip("How far the player can see in this zone (used for linear fog end distance).")]
        public float viewDistance = 20f;

        [Tooltip("Optional name for debugging and gizmos.")]
        public string name = "Zone";
    }

    [Header("Zones (each zone requires 4 corner Transforms)")]
    public Zone[] zones = new Zone[0];

    [Header("Subject to test for zone membership")]
    [Tooltip("If null the script will use Camera.main.transform at runtime.")]
    public Transform subject;

    [Header("Overlay / Fog")]
    [Tooltip("Time in seconds to fade the overlay and fog when entering/exiting a zone.")]
    public float transitionDuration = 0.25f;

    [Tooltip("If true the script will also apply linear RenderSettings.fog to limit view distance. Uses fog mode Linear.")]
    public bool enableFog = true;

    // runtime objects
    Canvas overlayCanvas;
    Image overlayImage;

    // state
    int currentZoneIndex = -1;
    float currentAlpha = 0f;
    float targetAlpha = 0f;
    float alphaVelocity = 0f;
    float fogLerp = 0f;
    float targetFogEnd = 0f;
    float startFogEnd = 0f;

    // cached original fog settings so leaving zones restores them
    bool originalFogEnabled;
    FogMode originalFogMode;
    float originalFogStart;
    float originalFogEnd;
    Color originalFogColor;

    void Awake()
    {
        CreateOverlay();
        CacheOriginalFog();
    }

    void Start()
    {
        // fallback subject
        if (subject == null && Camera.main != null)
            subject = Camera.main.transform;

        // validate zone arrays: ensure each zone has 4 corners (do not modify user data, just warn)
        if (zones == null) zones = new Zone[0];
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null) continue;
            if (zones[i].corners == null || zones[i].corners.Length != 4)
                Debug.LogWarningFormat(this, "Zone[{0}] ('{1}') does not have exactly 4 corners assigned.", i, zones[i].name);
        }
    }

    void CreateOverlay()
    {
        // Create a top-level canvas and fullscreen image at runtime so user does not need to add anything manually.
        var canvasGO = new GameObject("DarkScreenOverlayCanvas");
        canvasGO.transform.SetParent(transform, false);
        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 10000;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("DarkOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        overlayImage = imgGO.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);

        var rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // start invisible
        currentAlpha = 0f;
        targetAlpha = 0f;
    }

    void CacheOriginalFog()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogColor = RenderSettings.fogColor;
    }

    void Update()
    {
        if (subject == null)
        {
            // try to recover camera each frame if subject was not set initially
            if (Camera.main != null)
                subject = Camera.main.transform;
            if (subject == null)
                return; // nothing to test
        }

        // find which zone (if any) contains the subject. If multiple zones overlap, the first match in the array wins.
        int foundIndex = -1;
        Vector3 p = subject.position;
        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            if (z == null || z.corners == null || z.corners.Length != 4) continue;
            if (IsPointInQuadXZ(p, z.corners))
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex != currentZoneIndex)
        {
            // zone changed
            currentZoneIndex = foundIndex;
            if (currentZoneIndex >= 0)
            {
                // entering a zone
                targetAlpha = Mathf.Clamp01(zones[currentZoneIndex].darkness);
                if (enableFog)
                {
                    startFogEnd = RenderSettings.fogEndDistance;
                    targetFogEnd = Mathf.Max(0.1f, zones[currentZoneIndex].viewDistance);
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    // compute a reasonable start distance (halfway)
                    RenderSettings.fogStartDistance = Mathf.Max(0f, targetFogEnd * 0.5f);
                    // fogEnd will be lerped for a smooth transition
                }
            }
            else
            {
                // leaving all zones: restore
                targetAlpha = 0f;
                if (enableFog)
                {
                    // restore fog settings
                    targetFogEnd = originalFogEnd;
                    RenderSettings.fogStartDistance = originalFogStart;
                    RenderSettings.fogMode = originalFogMode;
                    // we'll toggle fogEnabled at end of lerp when restored to original
                }
            }
        }

        // Smooth alpha lerp
        if (transitionDuration > 0f)
        {
            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, transitionDuration);
        }
        else
        {
            currentAlpha = targetAlpha;
        }

        overlayImage.color = new Color(0f, 0f, 0f, currentAlpha);

        // Fog lerp: simple linear interpolation for fogEndDistance
        if (enableFog)
        {
            if (transitionDuration > 0f)
            {
                // approach targetFogEnd over transitionDuration
                fogLerp = Mathf.MoveTowards(fogLerp, 1f, Time.deltaTime / Mathf.Max(0.0001f, transitionDuration));
                float lerped = Mathf.Lerp(startFogEnd, targetFogEnd, fogLerp);
                RenderSettings.fogEndDistance = lerped;

                // if we finished restoring to original and originalFogEnabled was false, disable fog
                if (Mathf.Approximately(lerped, originalFogEnd) && currentZoneIndex == -1 && !originalFogEnabled)
                {
                    RenderSettings.fog = false;
                }
            }
            else
            {
                RenderSettings.fogEndDistance = targetFogEnd;
                if (currentZoneIndex == -1 && !originalFogEnabled)
                    RenderSettings.fog = false;
            }
        }
    }

    // Point-in-polygon test specialized for a convex quad on XZ plane using winding rule (works for any polygon too)
    bool IsPointInQuadXZ(Vector3 point, Transform[] corners)
    {
        // build 2D array of polygon points (xz)
        int n = corners.Length;
        Vector2[] poly = new Vector2[n];
        for (int i = 0; i < n; i++)
            poly[i] = new Vector2(corners[i].position.x, corners[i].position.z);

        Vector2 p = new Vector2(point.x, point.z);
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];
            // raycast crossing test
            bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                              (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + Mathf.Epsilon) + pi.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    void OnDrawGizmos()
    {
        if (zones == null) return;

        for (int zi = 0; zi < zones.Length; zi++)
        {
            var z = zones[zi];
            if (z == null || z.corners == null || z.corners.Length != 4) continue;

            // draw quad lines
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 4; i++)
            {
                var a = z.corners[i];
                var b = z.corners[(i + 1) % 4];
                if (a != null && b != null)
                    Gizmos.DrawLine(a.position, b.position);
            }

            // draw small spheres on corners
            Gizmos.color = Color.yellow;
            foreach (var c in z.corners)
                if (c != null)
                    Gizmos.DrawSphere(c.position, 0.05f);

            // label
#if UNITY_EDITOR
            var first = z.corners[0];
            if (first != null)
                UnityEditor.Handles.Label(first.position + Vector3.up * 0.2f, z.name);
#endif
        }
    }

    void OnDestroy()
    {
        // restore fog to original when script is destroyed
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fogColor = originalFogColor;
    }
}