
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walking speed (units/sec).")]
    [SerializeField] private float walkSpeed = 4f;            // increased for slightly faster walking
    [Tooltip("Sprinting speed (units/sec).")]
    [SerializeField] private float sprintSpeed = 9f;          // increased for faster sprinting
    [Tooltip("Crouching speed (units/sec).")]
    [SerializeField] private float crouchSpeed = 1.5f;
    [Tooltip("Rotation smoothing factor. Lower = slower turning.")]
    [SerializeField] private float rotationSmooth = 4f;       // reduced to make turning noticeably slower

    private float currentSpeed;

    [Header("Input")]
    [Tooltip("Key used to sprint.")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [Tooltip("Key used to crouch.")]
    [SerializeField] private KeyCode crouchKey = KeyCode.Q;
    [Tooltip("When true crouch is hold; when false crouch toggles on key press.")]
    [SerializeField] private bool crouchHold = true;

    [Header("States")]
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private bool isCrouching = false;

    [Header("Animation")]
    [Tooltip("Animator controlling player animations. If left empty this component will attempt to resolve one on the same GameObject.")]
    [SerializeField] private Animator anim;

    [Header("Camera")]
    [Tooltip("Optional camera controller to notify about sprinting for FOV changes.")]
    [SerializeField] private AdvancedCameraController cameraController;

    private Vector3 inputDirection = Vector3.zero;

    // Optional Rigidbody-based movement (preferred for physics-driven characters)
    private Rigidbody rb;

    void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (cameraController == null)
            cameraController = FindObjectOfType<AdvancedCameraController>();

        if (anim == null)
            Debug.LogWarning("PlayerMovement: Animator is not assigned and was not found on the same GameObject.", this);

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Keep input and animation in Update for best responsiveness.
        ReadInput();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        // Perform movement and rotation in FixedUpdate to keep physics stable and avoid timestep jitter.
        HandleMovement(Time.fixedDeltaTime);
    }

    // Read raw player input and update state flags.
    // This implementation never calls legacy Input.* unguarded: it tries legacy calls and falls back to the new Input System if legacy is unavailable.
    private void ReadInput()
    {
        Vector2 move2D = Vector2.zero;
        bool legacySucceeded = false;

        // Attempt legacy Input calls inside try/catch to avoid InvalidOperationException when Player Settings use the new system.
        try
        {
            // These calls will throw InvalidOperationException if the legacy Input Manager is not active at runtime.
            move2D.x = Input.GetAxisRaw("Horizontal");
            move2D.y = Input.GetAxisRaw("Vertical");

            // Sprint: hold sprintKey while moving (and not crouching)
            isSprinting = Input.GetKey(sprintKey) && !isCrouching && (Mathf.Abs(move2D.x) + Mathf.Abs(move2D.y)) > 0.1f;

            // Crouch: either hold or toggle based on inspector setting
            if (crouchHold)
                isCrouching = Input.GetKey(crouchKey);
            else if (Input.GetKeyDown(crouchKey))
                isCrouching = !isCrouching;

            legacySucceeded = true;
        }
        catch (InvalidOperationException)
        {
            // Legacy Input not available at runtime — we'll fallback below.
            legacySucceeded = false;
        }

        if (!legacySucceeded)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // New Input System fallback (polled). Safe because it's compile-time guarded.
            move2D = ReadNewInputAxes();

            // sprint/crouch via new input system (fixed mapping for common keys)
            if (Keyboard.current != null)
            {
                // Sprint: leftShift held and not crouching and has movement
                isSprinting = Keyboard.current.leftShiftKey.isPressed && !isCrouching && move2D.sqrMagnitude > 0.01f;

                // Crouch: support hold or toggle using 'Q' key (matches default inspector crouchKey but new input system mapping is explicit)
                if (crouchHold)
                    isCrouching = Keyboard.current.qKey.isPressed;
                else if (Keyboard.current.qKey.wasPressedThisFrame)
                    isCrouching = !isCrouching;
            }
            else
            {
                isSprinting = false;
                // cannot change crouch if keyboard not available
            }
#else
            // No input backend available at runtime; keep defaults and avoid any calls that throw.
            move2D = Vector2.zero;
            isSprinting = false;
            // Crouch cannot be changed without an input backend.
#endif
        }

        // Normalize to keep diagonal speed consistent
        if (move2D.sqrMagnitude > 1f)
            move2D.Normalize();

        inputDirection = new Vector3(move2D.x, 0f, move2D.y);
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private Vector2 ReadNewInputAxes()
    {
        Vector2 m = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) m.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) m.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) m.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) m.x -= 1f;
        }

        if (Gamepad.current != null)
            m += Gamepad.current.leftStick.ReadValue();

        return m;
    }
#endif

    // Movement now accepts deltaTime and runs inside FixedUpdate for consistent physics behavior.
    private void HandleMovement(float deltaTime)
    {
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isSprinting)
            currentSpeed = sprintSpeed;
        else
            currentSpeed = walkSpeed;

        if (inputDirection.magnitude > 0.01f)
        {
            Vector3 moveWorld = transform.TransformDirection(inputDirection) * currentSpeed * deltaTime;

            if (rb != null && !rb.isKinematic)
            {
                // Rigidbody-driven movement: use MovePosition for stable physics interaction.
                rb.MovePosition(rb.position + moveWorld);

                // Smooth rotation using physics-friendly approach
                Quaternion targetRot = Quaternion.LookRotation(moveWorld.normalized);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSmooth * deltaTime));
            }
            else
            {
                // Transform-driven movement (fallback)
                transform.position += moveWorld;

                Quaternion targetRot = Quaternion.LookRotation(moveWorld.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * deltaTime);
            }
        }

        // Notify camera controller safely (keep in sync with movement)
        if (cameraController != null)
            cameraController.isSprinting = isSprinting;
    }

    private void HandleAnimations()
    {
        if (anim == null) return;

        bool isWalking = inputDirection.magnitude > 0.1f && !isSprinting && !isCrouching;
        anim.SetBool("IsWalking", isWalking);
        anim.SetBool("IsSprinting", isSprinting);
        anim.SetBool("IsCrouching", isCrouching);
    }

    public bool GetIsSprinting() => isSprinting;
    public bool GetIsCrouching() => isCrouching;
    public Vector3 GetInputDirection() => inputDirection;
}