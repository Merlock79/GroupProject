using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Attach to any GameObject (for example a square zone). If the GameObject has tag "Lighter"
/// this component creates and manages a realistic Spotlight that shines from a chosen origin
/// (you can set the origin Transform to any side/point). Multiple Lighter components can exist.
/// 
/// Features:
/// - Spot light with adjustable color, intensity, range and angle
/// - Soft shadows option (casts shadows if supported)
/// - Cookie texture support for more realistic projection
/// - Optional subtle flicker and smooth follow of a target
/// - Editor gizmo shows cone and origin so you can position visually in the Scene view
/// 
/// Usage:
/// - Add this component to the object that should be lit (or any object).
/// - Set the same GameObject tag to "Lighter" if you want the script to auto-enable only for tagged objects.
/// - Assign a `lightOrigin` transform (empty placed on one side) to select the origin point and direction.
/// - Tune color, range, intensity, spotAngle, cookie, shadows, flicker in inspector.
/// </summary>
[DisallowMultipleComponent]
public class Lighter : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("If true the light will only be active when this GameObject's tag is exactly 'Lighter'.")]
    public bool requireTag = true;

    [Header("Light origin & target")]
    [Tooltip("Transform that defines the light origin and forward direction. If null this.transform is used.")]
    public Transform lightOrigin;
    [Tooltip("Optional transform the light should point toward. If null, lightOrigin.forward is used.")]
    public Transform aimTarget;

    [Header("Light properties")]
    public Color color = Color.white;
    [Range(0f, 20f)]
    public float intensity = 2f;
    [Range(0.5f, 100f)]
    public float range = 15f;
    [Range(1f, 90f)]
    public float spotAngle = 45f;
    [Tooltip("Cookie texture projected by the spotlight (optional).")]
    public Texture cookie;

    [Header("Shadows")]
    [Tooltip("If true the light will cast soft shadows (expensive).")]
    public bool castShadows = true;
    [Range(0f, 1f)]
    public float shadowStrength = 0.8f;
    [Tooltip("Shadow resolution fallback setting.")]
    public LightShadowResolution shadowResolution = LightShadowResolution.Medium;

    [Header("Behavior")]
    [Tooltip("If true the light will smoothly follow the aimTarget's position/direction.")]
    public bool smoothFollow = true;
    [Tooltip("Smooth follow speed. Larger is snappier.")]
    [Range(1f, 50f)]
    public float followSpeed = 12f;
    [Tooltip("Optional subtle flicker to simulate a handheld lighter. Set to 0 to disable.")]
    [Range(0f, 1f)]
    public float flickerAmount = 0.02f;
    [Tooltip("Flicker speed multiplier.")]
    [Range(0.1f, 10f)]
    public float flickerSpeed = 4f;

    [Header("Runtime")]
    [Tooltip("Created Light GameObject (read-only).")]
    public Light runtimeLight;

    [Header("Gizmos")]
    public bool drawGizmo = true;
    public Color gizmoColor = new Color(1f, 0.9f, 0.6f, 0.6f);

    // internal
    Transform _origin;
    float _baseIntensity;
    float _flickerOffset;

    void Reset()
    {
        // sensible defaults
        intensity = 2f;
        range = 15f;
        spotAngle = 45f;
    }

    void Awake()
    {
        _flickerOffset = UnityEngine.Random.value * 100f;

        if (lightOrigin == null)
            lightOrigin = transform;

        _origin = lightOrigin;

        // Only create light if tag requirement is satisfied (or not required)
        if (!requireTag || CompareTag("Lighter"))
            CreateRuntimeLight();
    }

    void OnEnable()
    {
        if (runtimeLight == null && (!requireTag || CompareTag("Lighter")))
            CreateRuntimeLight();
    }

    void OnDisable()
    {
        if (runtimeLight != null)
            runtimeLight.enabled = false;
    }

    void OnDestroy()
    {
        if (runtimeLight != null)
            DestroyImmediate(runtimeLight.gameObject);
    }

    void CreateRuntimeLight()
    {
        if (runtimeLight != null) return;

        GameObject go = new GameObject($"{name}_SpotLight");
        go.transform.SetParent(_origin, true);
        go.transform.position = _origin.position;
        if (aimTarget != null)
            go.transform.LookAt(aimTarget.position);
        else
            go.transform.forward = _origin.forward;

        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.spotAngle = spotAngle;
        light.cookie = cookie;
        light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = shadowStrength;
        light.shadowResolution = shadowResolution;
        // enable per-pixel shadow if available/desired (platform dependent)
#if UNITY_2019_1_OR_NEWER
        light.shadowCustomResolution = 0; // leave to quality settings
#endif
        runtimeLight = light;
        _baseIntensity = intensity;
    }

    void Update()
    {
        if (requireTag && !CompareTag("Lighter"))
        {
            if (runtimeLight != null && runtimeLight.enabled)
                runtimeLight.enabled = false;
            return;
        }

        if (runtimeLight == null)
            CreateRuntimeLight();

        if (runtimeLight == null) return;

        // Update base properties (allow runtime tuning from inspector)
        runtimeLight.color = color;
        runtimeLight.spotAngle = spotAngle;
        runtimeLight.range = range;
        runtimeLight.shadowStrength = shadowStrength;
        runtimeLight.cookie = cookie;
        runtimeLight.shadows = castShadows ? LightShadows.Soft : LightShadows.None;

        // Position & orientation
        if (_origin != null)
        {
            // desired rotation: aim at aimTarget or forward direction
            Quaternion desiredRot;
            if (aimTarget != null)
                desiredRot = Quaternion.LookRotation((aimTarget.position - _origin.position).normalized, Vector3.up);
            else
                desiredRot = _origin.rotation;

            // desired position: origin position
            Vector3 desiredPos = _origin.position;

            if (smoothFollow)
            {
                runtimeLight.transform.position = Vector3.Lerp(runtimeLight.transform.position, desiredPos, Mathf.Clamp01(Time.deltaTime * followSpeed));
                runtimeLight.transform.rotation = Quaternion.Slerp(runtimeLight.transform.rotation, desiredRot, Mathf.Clamp01(Time.deltaTime * followSpeed));
            }
            else
            {
                runtimeLight.transform.position = desiredPos;
                runtimeLight.transform.rotation = desiredRot;
            }
        }

        // Flicker
        if (flickerAmount > 0f)
        {
            float flick = (Mathf.PerlinNoise(Time.time * flickerSpeed + _flickerOffset, 0f) - 0.5f) * 2f;
            float inst = Mathf.Max(0f, _baseIntensity + flick * flickerAmount * _baseIntensity);
            runtimeLight.intensity = inst;
        }
        else
        {
            runtimeLight.intensity = _baseIntensity;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;

        Transform gt = (lightOrigin != null) ? lightOrigin : transform;
        if (gt == null) return;

        Gizmos.color = gizmoColor;
        Vector3 pos = gt.position;
        Vector3 dir;
        if (aimTarget != null)
            dir = (aimTarget.position - pos).normalized;
        else
            dir = gt.forward;

        // Draw cone
        float len = Mathf.Max(0.1f, range);
        float halfAngle = Mathf.Deg2Rad * Mathf.Max(0.1f, spotAngle) * 0.5f;
        int steps = 20;
        Vector3 prev = pos + Quaternion.AngleAxis(-spotAngle * 0.5f, Vector3.up) * dir * len;
        for (int i = 1; i <= steps; i++)
        {
            float a = -spotAngle * 0.5f + (spotAngle * i) / (float)steps;
            Vector3 next = pos + Quaternion.AngleAxis(a, Vector3.up) * dir * len;
            Gizmos.DrawLine(pos, next);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // small sphere at origin
        Gizmos.DrawSphere(pos, Mathf.Max(0.02f, len * 0.01f));
    }

#if UNITY_EDITOR
    // Allow immediate creation in the editor when tag or origin changes
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (lightOrigin == null)
                lightOrigin = transform;
        }
    }
#endif
}