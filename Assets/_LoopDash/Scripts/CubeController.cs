using UnityEngine;

[RequireComponent(typeof(CubeMovement), typeof(HazardDetector))]
public class CubeController : MonoBehaviour
{
    [SerializeField] private CubeMovement movement;
    [SerializeField] private HazardDetector hazardDetector;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<CubeMovement>();
        if (hazardDetector == null) hazardDetector = GetComponent<HazardDetector>();
    }

    private void OnEnable()
    {
        hazardDetector.OnHazardDetected += HandleHazard;
    }

    private void OnDisable()
    {
        hazardDetector.OnHazardDetected -= HandleHazard;
    }

    private void HandleHazard(GameObject hazard)
    {
        if (IsDead) return;
        IsDead = true;

        movement.enabled = false;

        Debug.Log($"Гравець загинув: зіткнення з {hazard.name}", hazard);
    }
}
