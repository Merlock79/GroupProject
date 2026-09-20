using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.25f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float rotationSharpness = 14f;
    [SerializeField] private float gravity = -20f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 4.2f;
    [SerializeField] private float deceleration = 8f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.15f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Input")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchInputKey = KeyCode.LeftControl;

    private CharacterController characterController;
    private Animator animator;
    private Transform cameraTransform;
    private AdvancedCameraController cameraController;
    private float standingCenterY;
    private float verticalVelocity;
    private float currentMoveSpeed;
    private float movementRestriction;
    private bool movementEnabled = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        standingCenterY = characterController.center.y;

        animator = GetComponentInChildren<Animator>();
        ResolveCamera();

        if (animator == null)
        {
            Debug.LogError("PlayerMovement requires an Animator on the player model.", this);
        }
    }

    private void Update()
    {
        if (!movementEnabled)
        {
            currentMoveSpeed = 0f;
            UpdateAnimator(0f, false, false);
            return;
        }

        bool crouching = Input.GetKey(crouchInputKey);
        UpdateCrouch(crouching);
        Move(crouching);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        verticalVelocity = 0f;
        currentMoveSpeed = 0f;
    }

    public void SetMovementRestriction(float restriction)
    {
        movementRestriction = Mathf.Clamp01(restriction);
    }

    private void UpdateCrouch(bool crouching)
    {
        float targetHeight = crouching ? crouchingHeight : standingHeight;
        characterController.height = Mathf.MoveTowards(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = characterController.center;
        center.y = standingCenterY - (standingHeight - characterController.height) * 0.5f;
        characterController.center = center;
    }

    private void Move(bool crouching)
    {
        ResolveCamera();

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        Vector3 movement = forward * input.y + right * input.x;
        bool sprintRequested = Input.GetKey(sprintKey) && !crouching;
        float movementMultiplier = 1f - movementRestriction;
        float topSpeed = (crouching ? crouchSpeed : sprintRequested ? sprintSpeed : walkSpeed) * movementMultiplier;
        float targetMoveSpeed = movement.sqrMagnitude > 0.001f ? topSpeed : 0f;
        float accelerationMultiplier = Mathf.Lerp(1f, 0.2f, movementRestriction);
        float decelerationMultiplier = Mathf.Lerp(1f, 2.5f, movementRestriction);
        float speedChange = targetMoveSpeed > currentMoveSpeed
            ? acceleration * accelerationMultiplier
            : deceleration * decelerationMultiplier;
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetMoveSpeed, speedChange * Time.deltaTime);

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            float effectiveRotationSharpness = rotationSharpness * movementMultiplier;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, effectiveRotationSharpness * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move((movement * currentMoveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

        bool sprinting = sprintRequested && movement.sqrMagnitude > 0.001f;
        UpdateAnimator(GetHorizontalSpeed(), sprinting, crouching);
    }

    private float GetHorizontalSpeed()
    {
        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;
        return velocity.magnitude;
    }

    private void UpdateAnimator(float speed, bool sprinting, bool crouching)
    {
        if (animator != null)
            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);

        if (cameraController != null)
            cameraController.SetMovementState(sprinting, crouching);
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null && cameraController != null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        cameraTransform = mainCamera.transform;
        cameraController = mainCamera.GetComponent<AdvancedCameraController>();
    }
}
