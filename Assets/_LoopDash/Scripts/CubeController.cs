using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeController : MonoBehaviour
{
    [SerializeField] private CubeMovement movement;
    [SerializeField] private HazardDetector hazardDetector;

    private Rigidbody rb;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Debug.Log($"Гравець загинув: зіткнення з {hazard.name}", hazard);
    }
}