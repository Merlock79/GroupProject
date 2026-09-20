using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AdvancedCameraController : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float targetHeight = 1.55f;
    [SerializeField] private float normalDistance = 4f;
    [SerializeField] private float zoomDistance = 2f;
    [SerializeField] private float fpsDistance = 0.1f;
    [SerializeField] private float shoulderOffset = 0.55f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.18f;
    [SerializeField] private float collisionOffset = 0.08f;

    [Header("FOV")]
    [SerializeField] private float normalFOV = 70f;
    [SerializeField] private float sprintFOV = 85f;
    [SerializeField] private float fovSmooth = 8f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 0.6f;
    [SerializeField] private float crouchSmooth = 10f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private float shakeDuration = 0.15f;

    private Transform player;
    private Camera playerCamera;
    private float yaw;
    private float pitch = 15f;
    private float currentDistance;
    private float currentCrouch;
    private float shakeTimer;
    private float shoulderDirection = 1f;
    private bool isSprinting;
    private bool isCrouching;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            yaw = player.eulerAngles.y;

        currentDistance = normalDistance;
        playerCamera.fieldOfView = normalFOV;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentDistance = fpsDistance;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            currentDistance = zoomDistance;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            currentDistance = normalDistance;

        if (Input.GetKeyDown(KeyCode.Q))
            shoulderDirection = -1f;
        else if (Input.GetKeyDown(KeyCode.E))
            shoulderDirection = 1f;

        currentCrouch = Mathf.MoveTowards(
            currentCrouch,
            isCrouching ? crouchHeight : 0f,
            crouchSmooth * Time.deltaTime);

        float targetFOV = isSprinting ? sprintFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovSmooth * Time.deltaTime);

        if (shakeTimer > 0f)
            shakeTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = player.position + Vector3.up * (targetHeight - currentCrouch);
        Vector3 desiredPosition = focusPoint + rotation * new Vector3(shoulderOffset * shoulderDirection, 0f, -currentDistance);
        Vector3 cameraDirection = desiredPosition - focusPoint;

        if (Physics.SphereCast(
                focusPoint,
                collisionRadius,
                cameraDirection.normalized,
                out RaycastHit hit,
                cameraDirection.magnitude,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            desiredPosition = focusPoint + cameraDirection.normalized * Mathf.Max(0.05f, hit.distance - collisionOffset);
        }

        if (shakeTimer > 0f)
            desiredPosition += Random.insideUnitSphere * shakeStrength;

        transform.SetPositionAndRotation(desiredPosition, rotation);
    }

    public void SetMovementState(bool sprinting, bool crouching)
    {
        isSprinting = sprinting;
        isCrouching = crouching;
    }

    public void TriggerShake()
    {
        shakeTimer = shakeDuration;
    }
}
