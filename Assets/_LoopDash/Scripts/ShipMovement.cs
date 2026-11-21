using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(HazardDetector))]
public class ShipMovement : MonoBehaviour
{
    [SerializeField] private float horizontalSpeed = 4.5f;
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float upwardThrust = 32f;
    [SerializeField] private float verticalDrag = 0.2f;
    [SerializeField] private float maxVerticalSpeed = 6f;
    [SerializeField] private float maxTilt = 20f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private Transform shipVisual;
    [SerializeField] private float boxDeathSpeed = 0.2f;

    private Rigidbody body;
    private BoxCollider box;
    private HazardDetector hazardDetector;
    private CubeCollision collision;
    private bool thrustHeld;
    private float verticalSpeed;

    public float VerticalSpeed => verticalSpeed;
    public float HorizontalSpeed => horizontalSpeed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        hazardDetector = GetComponent<HazardDetector>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        collision = new CubeCollision(gameObject.scene.GetPhysicsScene(), box, collisionMask);
        if (shipVisual == null) shipVisual = transform.Find("ShipVisual");
    }

    public void SetThrust(bool held) => thrustHeld = held;

    private void FixedUpdate()
    {
        Vector3 position = body.position;
        Collider hazard = collision.FindOverlappingHazard(position);
        if (hazard != null)
        {
            hazardDetector.ReportContact(hazard);
            return;
        }

        verticalSpeed += (thrustHeld ? upwardThrust : -gravity) * Time.fixedDeltaTime;
        verticalSpeed *= Mathf.Max(0f, 1f - verticalDrag * Time.fixedDeltaTime);
        verticalSpeed = Mathf.Clamp(verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);

        float dx = horizontalSpeed * Time.fixedDeltaTime;
        Collider wall = collision.FindHazardAlong(position, Vector3.right, dx, out float hazardDistance);
        if (wall != null)
        {
            position.x += hazardDistance;
            body.MovePosition(position);
            hazardDetector.ReportContact(wall);
            return;
        }

        float horizontalTravel = collision.DistanceToSolid(position, Vector3.right, dx, out RaycastHit wallHit);
        position.x += horizontalTravel;
        if (horizontalTravel + boxDeathSpeed < dx && wallHit.collider != null)
        {
            body.MovePosition(position);
            hazardDetector.ReportFatalContact(wallHit.collider);
            return;
        }

        float dy = verticalSpeed * Time.fixedDeltaTime;
        Vector3 direction = dy >= 0f ? Vector3.up : Vector3.down;
        float verticalTravel = collision.DistanceToSolid(position, direction, Mathf.Abs(dy), out _);
        position.y += Mathf.Sign(dy) * verticalTravel;
        body.MovePosition(position);
    }

    private void Update()
    {
        if (shipVisual == null) return;
        float angle = maxVerticalSpeed <= 0f ? 0f : Mathf.Clamp(verticalSpeed / maxVerticalSpeed, -1f, 1f) * maxTilt;
        shipVisual.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
