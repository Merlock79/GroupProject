using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(HazardDetector))]
public class CubeMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 4.5f;
    [SerializeField, Min(0f)] private float maxStepHeight = 0.08f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

    [Header("Jump")]
    [SerializeField, Min(0.01f)] private float jumpHeight = 2.5f;
    [SerializeField, Min(0.01f), Tooltip("Seconds from takeoff to the top of the jump.")]
    private float timeToApex = 0.45f;
    [SerializeField, Min(0.01f), Tooltip("Seconds from the top of the jump back to the takeoff height.")]
    private float timeFromApex = 0.35f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.06f;
    [SerializeField, Min(0f), Tooltip("Seconds to keep a jump press before landing.")]
    private float jumpBufferTime = 0.1f;

    private Rigidbody body;
    private HazardDetector hazardDetector;
    private CubeCollision collision;
    private Collider hazardHit;
    private float verticalSpeed;
    private float landingHeight;
    private double leftGroundAt = double.NegativeInfinity;
    private double jumpPressedAt = double.NegativeInfinity;
    private bool grounded;
    private bool landedThisStep;
    private bool jumpRequested;
    private bool jumpHeld;

    public bool IsAirborne => !grounded ||
        (landedThisStep && transform.position.y > landingHeight + CubeCollision.ContactTolerance);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.useGravity = false;
        hazardDetector = GetComponent<HazardDetector>();
    }

    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        collision = new CubeCollision(gameObject.scene.GetPhysicsScene(), box, collisionMask);
        grounded = collision.TryGetFloor(body.position, CubeCollision.ContactTolerance, out _);
    }

    public void Jump()
    {
        jumpRequested = true;
        jumpPressedAt = Time.timeAsDouble;
    }

    public void SetJumpHeld(bool held)
    {
        if (held && !jumpHeld)
        {
            Jump();
        }

        jumpHeld = held;
    }

    public bool IsGrounded() => grounded;

    private void FixedUpdate()
    {
        Vector3 position = body.position;
        hazardHit = collision.FindOverlappingHazard(position);
        if (hazardHit != null)
        {
            hazardDetector.ReportContact(hazardHit);
            return;
        }

        bool wasGrounded = grounded;
        double now = Time.fixedTimeAsDouble;
        bool withinCoyoteTime = coyoteTime > 0f && now - leftGroundAt <= coyoteTime;
        bool withinJumpBuffer = jumpBufferTime > 0f && now - jumpPressedAt <= jumpBufferTime;
        bool hasJumpPress = jumpRequested || withinJumpBuffer;
        bool jumpStarted = (grounded && jumpHeld) || (hasJumpPress && (grounded || withinCoyoteTime));
        jumpRequested = false;
        if (jumpStarted)
        {
            verticalSpeed = 2f * jumpHeight / timeToApex;
            leftGroundAt = double.NegativeInfinity;
            jumpPressedAt = double.NegativeInfinity;
        }

        grounded = false;
        Advance(ref position, Time.fixedDeltaTime, wasGrounded && !jumpStarted);
        // The interpolated visual can still be above the physics pose in the landing step.
        landedThisStep = !wasGrounded && grounded;
        landingHeight = position.y;
        if (landedThisStep && !jumpStarted && withinJumpBuffer)
        {
            // A press that was valid on landing can launch on the next physics step.
            jumpRequested = true;
        }

        if (wasGrounded && !grounded && !jumpStarted)
        {
            leftGroundAt = now;
        }
        else if (grounded)
        {
            leftGroundAt = double.NegativeInfinity;
        }

        body.MovePosition(position);
        if (hazardHit != null)
        {
            hazardDetector.ReportContact(hazardHit);
        }
    }

    private void Advance(ref Vector3 position, float duration, bool canStep)
    {
        if (verticalSpeed > 0f)
        {
            float riseGravity = 2f * jumpHeight / (timeToApex * timeToApex);
            float riseDuration = Mathf.Min(duration, verticalSpeed / riseGravity);
            float riseDistance = (verticalSpeed - 0.5f * riseGravity * riseDuration) * riseDuration;
            verticalSpeed = Mathf.Max(0f, verticalSpeed - riseGravity * riseDuration);

            MoveVertically(ref position, riseDistance);
            MoveHorizontally(ref position, speed * riseDuration, false);
            duration -= riseDuration;
        }

        if (duration <= 0f || hazardHit != null) return;

        float fallGravity = 2f * jumpHeight / (timeFromApex * timeFromApex);
        float fallDistance = (verticalSpeed - 0.5f * fallGravity * duration) * duration;
        verticalSpeed -= fallGravity * duration;

        // Move over the leading edge before resolving the descent onto it.
        MoveHorizontally(ref position, speed * duration, canStep);
        MoveVertically(ref position, fallDistance);
    }

    private void MoveHorizontally(ref Vector3 position, float displacement, bool canStep)
    {
        if (displacement == 0f || hazardHit != null) return;

        Vector3 direction = displacement > 0f ? Vector3.right : Vector3.left;
        float distance = Mathf.Abs(displacement);
        float travel = collision.DistanceToSolid(position, direction, distance, out _);
        if (travel < distance && canStep && maxStepHeight > 0f &&
            collision.TryGetStep(position, direction, distance, maxStepHeight, out float lift, out float drop))
        {
            MoveSegment(ref position, Vector3.up, lift);
            MoveSegment(ref position, direction, distance);
            MoveSegment(ref position, Vector3.down, drop);
            return;
        }

        MoveSegment(ref position, direction, travel);
    }

    private void MoveVertically(ref Vector3 position, float displacement)
    {
        if (displacement == 0f || hazardHit != null) return;

        Vector3 direction = displacement > 0f ? Vector3.up : Vector3.down;
        float travel = collision.DistanceToSolid(position, direction, Mathf.Abs(displacement), out RaycastHit hit);
        MoveSegment(ref position, direction, travel);

        if (hit.collider != null && hazardHit == null)
        {
            verticalSpeed = 0f;
            grounded = displacement < 0f && CubeCollision.IsFloor(hit);
        }
    }

    private void MoveSegment(ref Vector3 position, Vector3 direction, float distance)
    {
        if (hazardHit != null) return;

        hazardHit = collision.FindHazardAlong(position, direction, distance, out float hazardDistance);
        position += direction * (hazardHit != null ? hazardDistance : distance);
    }
}
