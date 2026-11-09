using UnityEngine;

[RequireComponent(typeof(CubeMovement))]
public class CubeRotation : MonoBehaviour
{
    private const float LandingAngleStep = 90f;

    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float rotationSpeed = 210f;
    [SerializeField, Min(0.01f), Tooltip("Time in seconds to align the visual after landing.")]
    private float landingAlignmentDuration = 0.12f;

    private CubeMovement movement;
    private bool wasAirborne;
    private Quaternion landingStartRotation;
    private Quaternion landingTargetRotation;
    private float landingProgress = 1f;

    private void Awake()
    {
        movement = GetComponent<CubeMovement>();
    }

    private void LateUpdate()
    {
        if (!movement.isActiveAndEnabled) return;

        bool isAirborne = movement.IsAirborne;
        if (isAirborne)
        {
            visual.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime, Space.Self);
        }
        else if (wasAirborne)
        {
            landingStartRotation = visual.localRotation;
            Vector3 angles = visual.localEulerAngles;
            angles.z = Mathf.Round(angles.z / LandingAngleStep) * LandingAngleStep;
            landingTargetRotation = Quaternion.Euler(angles);
            landingProgress = 0f;
        }
        else if (landingProgress < 1f)
        {
            landingProgress = Mathf.Clamp01(landingProgress + Time.deltaTime / landingAlignmentDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, landingProgress);
            visual.localRotation = Quaternion.Slerp(landingStartRotation, landingTargetRotation, easedProgress);
        }

        wasAirborne = isAirborne;
    }
}
