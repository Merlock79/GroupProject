using UnityEngine;

[RequireComponent(typeof(ShipMovement), typeof(HazardDetector))]
public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipMovement movement;
    private HazardDetector hazardDetector;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<ShipMovement>();
        hazardDetector = GetComponent<HazardDetector>();
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

        if (TryGetComponent(out ShipInput input)) input.enabled = false;
        if (TryGetComponent(out ShipWeapon weapon)) weapon.enabled = false;
        movement.enabled = false;
    }

    public void SetThrust(bool held)
    {
        if (movement != null) movement.SetThrust(held);
    }
}
