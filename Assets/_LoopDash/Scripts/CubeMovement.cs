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
    [SerializeField] private ParticleSystem extraJumpAura;

    private Rigidbody body;
    private HazardDetector hazardDetector;
    private CubeCollision collision;
    private Collider hazardHit;
    private float verticalSpeed;
    private float landingHeight;
    private float currentJumpHeight;
    private float pendingLaunchHeight;
    private double leftGroundAt = double.NegativeInfinity;
    private double jumpPressedAt = double.NegativeInfinity;
    private bool grounded;
    private bool landedThisStep;
    private bool jumpRequested;
    private bool jumpHeld;

    public bool HasExtraJump { get; private set; }
    public bool IsAirborne => !grounded ||
        (landedThisStep && transform.position.y > landingHeight + CubeCollision.ContactTolerance);

    public float CurrentJumpHeight => currentJumpHeight;
    public float HorizontalSpeed => speed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.useGravity = false;
        hazardDetector = GetComponent<HazardDetector>();
        currentJumpHeight = jumpHeight;
    }

    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        collision = new CubeCollision(gameObject.scene.GetPhysicsScene(), box, collisionMask);
        grounded = collision.TryGetFloor(body.position, CubeCollision.ContactTolerance, out _);
    }

    private void OnDisable()
    {
        pendingLaunchHeight = 0f;
        ClearExtraJump();
    }

    public void Jump()
    {
        jumpRequested = true;
        jumpPressedAt = Time.timeAsDouble;
    }

    public bool TryAddExtraJump()
    {
        if (!isActiveAndEnabled || HasExtraJump) return false;

        HasExtraJump = true;
        SetExtraJumpAura(true);
        return true;
    }

    public void ClearExtraJump()
    {
        HasExtraJump = false;
        SetExtraJumpAura(false);
    }

    /// <summary>
    /// Queues a normal-height launch for the next physics step.
    /// </summary>
    public void ApplyJump() => ApplyJump(jumpHeight);

    /// <summary>
    /// Queues a launch with the requested height, even when the cube is already airborne.
    /// </summary>
    public void ApplyJump(float height)
    {
        if (!isActiveAndEnabled || float.IsNaN(height) || float.IsInfinity(height) || height <= 0f) return;

        pendingLaunchHeight = height;
    }

    public void SetJumpHeld(bool held)
    {
        if (held && !jumpHeld)
        {
            Jump();
        }

        jumpHeld = held;
    }

    private void SetExtraJumpAura(bool visible)
    {
        if (extraJumpAura == null) return;

        if (visible)
        {
            extraJumpAura.gameObject.SetActive(true);
            extraJumpAura.Play(true);
        }
        else
        {
            if (extraJumpAura.gameObject.activeInHierarchy)
                extraJumpAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            extraJumpAura.gameObject.SetActive(false);
        }
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
        bool canGroundJump = wasGrounded || withinCoyoteTime;
        bool groundJumpRequested = (wasGrounded && jumpHeld) || (hasJumpPress && canGroundJump);
        bool useExtraJump = pendingLaunchHeight == 0f && jumpRequested && HasExtraJump && !canGroundJump;
        bool jumpStarted = pendingLaunchHeight > 0f || groundJumpRequested || useExtraJump;

        if (useExtraJump)
        {
            ClearExtraJump();
        }

        jumpRequested = false;
        if (jumpStarted)
        {
            currentJumpHeight = pendingLaunchHeight > 0f ? pendingLaunchHeight : jumpHeight;
            pendingLaunchHeight = 0f;
            verticalSpeed = 2f * currentJumpHeight / timeToApex;
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
        float horizontalDisplacement = speed * duration;
        float fallDuration = duration;
        bool startedRising = verticalSpeed > 0f;

        if (startedRising)
        {
            float riseGravity = 2f * currentJumpHeight / (timeToApex * timeToApex);
            float riseDuration = Mathf.Min(duration, verticalSpeed / riseGravity);
            float riseDistance = (verticalSpeed - 0.5f * riseGravity * riseDuration) * riseDuration;
            verticalSpeed = Mathf.Max(0f, verticalSpeed - riseGravity * riseDuration);

            MoveVertically(ref position, riseDistance);
            fallDuration -= riseDuration;
        }

        if ((fallDuration <= 0f && !startedRising) || hazardHit != null) return;

        float fallDistance = 0f;
        if (fallDuration > 0f)
        {
            float fallGravity = 2f * currentJumpHeight / (timeFromApex * timeFromApex);
            fallDistance = (verticalSpeed - 0.5f * fallGravity * fallDuration) * fallDuration;
            verticalSpeed -= fallGravity * fallDuration;
        }

        // Keep horizontal travel fixed for the whole physics step, including across the apex.
        MoveHorizontally(ref position, horizontalDisplacement, canStep && !startedRising);
        if (fallDuration > 0f) MoveVertically(ref position, fallDistance);
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
