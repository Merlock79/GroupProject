using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class AdvancedCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player transform used for camera targeting.")]
    [SerializeField] private Transform player;
    [Tooltip("Camera used for FOV changes. If null, will try to find one on Start.")]
    [SerializeField] private Camera cam;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private float minY = -40f;
    [SerializeField] private float maxY = 70f;
    private float rotX;
    private float rotY;

    [Header("Camera Distances")]
    [SerializeField] private float normalDistance = 4f;
    [SerializeField] private float zoomDistance = 2f;
    [SerializeField] private float fpsDistance = 0.1f;
    private float currentDistance;

    [Header("Shoulder Switching")]
    [SerializeField] private float shoulderOffset = 1.2f;
    private float currentShoulder = 1f; // 1 = right, -1 = left

    [Header("Collision")]
    [SerializeField] private float collisionOffset = 0.2f;
    [SerializeField] private LayerMask collisionMask;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 10f;
    [SerializeField] private float rotationSmooth = 12f;

    [Header("Dynamic FOV")]
    [SerializeField] private float normalFOV = 70f;
    [SerializeField] private float sprintFOV = 85f;
    [SerializeField] private float fovSmooth = 8f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Camera Shake")]
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private float shakeDuration = 0.15f;
    private float shakeTimer = 0f;

    // Exposed read/write so PlayerMovement can inform the camera of sprinting.
    internal bool isSprinting;

    void Start()
    {
        // Ensure camera reference
        if (cam == null)
            cam = GetComponentInChildren<Camera>() ?? Camera.main;

        if (cam == null)
            Debug.LogWarning("AdvancedCameraController: no Camera assigned or found.", this);

        if (player == null)
            Debug.LogWarning("AdvancedCameraController: Player transform is not assigned.", this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = normalDistance;

        if (cam != null)
            cam.fieldOfView = normalFOV;

        // Initialize rotation based on current transform / player orientation for smooth first frame
        rotY = transform.eulerAngles.y;
        rotX = transform.eulerAngles.x;
        if (player != null)
            rotY = player.eulerAngles.y;
    }

    void Update()
    {
        HandleViewModeSwitch();
        HandleMouseLook();
        HandleShoulderSwitch();
        HandleFOV();
        HandleShakeTimer();
    }

    void LateUpdate()
    {
        HandleCameraPosition();
    }

    // ----- Input abstraction: supports legacy Input and new Input System (via scripting symbols) -----
    private bool KeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null) return false;
        switch (key)
        {
            case KeyCode.Alpha1: return Keyboard.current.digit1Key.wasPressedThisFrame;
            case KeyCode.Alpha2: return Keyboard.current.digit2Key.wasPressedThisFrame;
            case KeyCode.Alpha3: return Keyboard.current.digit3Key.wasPressedThisFrame;
            case KeyCode.E: return Keyboard.current.eKey.wasPressedThisFrame;
            case KeyCode.Q: return Keyboard.current.qKey.wasPressedThisFrame;
            case KeyCode.C: return Keyboard.current.cKey.wasPressedThisFrame;
            case KeyCode.LeftShift: return Keyboard.current.leftShiftKey.wasPressedThisFrame;
            case KeyCode.RightShift: return Keyboard.current.rightShiftKey.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }

    private bool Key(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current == null) return false;
        switch (key)
        {
            case KeyCode.LeftShift: return Keyboard.current.leftShiftKey.isPressed;
            case KeyCode.RightShift: return Keyboard.current.rightShiftKey.isPressed;
            case KeyCode.E: return Keyboard.current.eKey.isPressed;
            case KeyCode.Q: return Keyboard.current.qKey.isPressed;
            default: return false;
        }
#else
        return Input.GetKey(key);
#endif
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.delta.ReadValue();
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    // ----- View / mode switching -----
    void HandleViewModeSwitch()
    {
        if (KeyDown(KeyCode.Alpha1))
            currentDistance = fpsDistance;
        else if (KeyDown(KeyCode.Alpha2))
            currentDistance = zoomDistance;
        else if (KeyDown(KeyCode.Alpha3))
            currentDistance = normalDistance;
    }

    // ----- Mouse look -----
    void HandleMouseLook()
    {
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        rotY += mouseX;
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, minY, maxY);
    }

    // ----- Shoulder swap -----
    void HandleShoulderSwitch()
    {
        if (KeyDown(KeyCode.E))
            currentShoulder = 1f;

        if (KeyDown(KeyCode.Q))
            currentShoulder = -1f;
    }

    // ----- FOV (uses either the isSprinting flag or input) -----
    void HandleFOV()
    {
        bool sprintInput = Key(sprintKey);
        float targetFOV = (isSprinting || sprintInput) ? sprintFOV : normalFOV;

        if (cam != null)
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
    }

    void HandleShakeTimer()
    {
        if (shakeTimer > 0f)
            shakeTimer -= Time.deltaTime;
    }

    public void TriggerShake()
    {
        shakeTimer = shakeDuration;
    }

    // ----- Positioning, collision and smoothing -----
    void HandleCameraPosition()
    {
        if (player == null)
            return;

        Quaternion rotation = Quaternion.Euler(rotX, rotY, 0f);
        Vector3 targetPos = player.position + Vector3.up * 1.7f;
        Vector3 shoulderPos = rotation * Vector3.right * currentShoulder * shoulderOffset;

        Vector3 desiredCameraPos = targetPos + shoulderPos - rotation * Vector3.forward * currentDistance;

        // Collision check (clamp adjustedDistance to positive)
        if (Physics.Linecast(targetPos, desiredCameraPos, out RaycastHit hit, collisionMask))
        {
            float adjustedDistance = Mathf.Max(0.05f, hit.distance - collisionOffset);
            desiredCameraPos = targetPos + shoulderPos - rotation * Vector3.forward * adjustedDistance;
        }

        // Camera shake
        if (shakeTimer > 0f)
            desiredCameraPos += Random.insideUnitSphere * shakeStrength;

        // Smooth movement & rotation
        transform.position = Vector3.Lerp(transform.position, desiredCameraPos, Time.deltaTime * positionSmooth);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * rotationSmooth);
    }

    // Public helpers
    public void SetShoulder(bool right) => currentShoulder = right ? 1f : -1f;
    public void SetDistance(float distance) => currentDistance = distance;
}