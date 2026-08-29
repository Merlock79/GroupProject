using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Камера")]
    public Camera playerCamera; // перетащи сюда камеру-ребёнка игрока
    public float mouseSensitivity = 2f;
    private float verticalLookRotation = 0f;

    [Header("Движение")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 6.5f;
    public float crouchSpeed = 1.8f;
    public float gravity = -20f;
    private CharacterController controller;
    private Vector3 velocity;

    [Header("Приседание (крадущийся шаг)")]
    public float standingHeight = 2f;
    public float crouchHeight = 1.2f;
    public float crouchTransitionSpeed = 8f; // насколько быстро камера/коллайдер приседают
    private bool isCrouching = false;
    private Vector3 standingCameraLocalPos;
    private Vector3 crouchingCameraLocalPos;
    private float baseCenterY; // Y центра коллайдера при полный рост, берём из инспектора как есть

    [Header("Стамина (выносливость для бега)")]
    [Tooltip("Сколько секунд можно бежать без перерыва на полной стамине")]
    public float maxStamina = 10f;
    [Tooltip("Через сколько секунд после остановки бега стамина начнёт восстанавливаться")]
    public float staminaRegenDelay = 1.5f;
    [Tooltip("Сколько секунд стамины восстанавливается в секунду реального времени (например 0.5 = вдвое медленнее, чем тратится)")]
    public float staminaRegenRate = 2f;
    private float currentStamina;
    private float regenDelayTimer;

    [Header("Взаимодействие")]
    public float interactRange = 2.5f;
    public LayerMask interactLayer = ~0; // по умолчанию все слои, можешь выставить отдельный слой "Interactable"
    public KeyCode interactKey = KeyCode.E;

    private Generator currentGenerator; // генератор, на который сейчас смотрим

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controller.height = standingHeight;
        baseCenterY = controller.center.y; // сохраняем как есть в инспекторе, ничего не пересчитываем сами
        standingCameraLocalPos = playerCamera.transform.localPosition;
        crouchingCameraLocalPos = standingCameraLocalPos - new Vector3(0, standingHeight - crouchHeight, 0);

        currentStamina = maxStamina;
    }

    void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleStamina();
        HandleMove();
        HandleInteraction();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // поворот тела влево/вправо
        transform.Rotate(Vector3.up * mouseX);

        // наклон камеры вверх/вниз с ограничением
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    void HandleCrouch()
    {
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // центр коллайдера подстраиваем, сохраняя нижнюю точку капсулы на месте
        // (не жёстко height/2, а относительно того, что было настроено изначально при полном росте)
        Vector3 center = controller.center;
        center.y = baseCenterY - (standingHeight - controller.height) / 2f;
        controller.center = center;

        Vector3 targetCamPos = isCrouching ? crouchingCameraLocalPos : standingCameraLocalPos;
        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition, targetCamPos, crouchTransitionSpeed * Time.deltaTime);
    }

    void HandleStamina()
    {
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
        bool isSprintingNow = wantsSprint && isMoving && currentStamina > 0f;

        if (isSprintingNow)
        {
            currentStamina -= Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            regenDelayTimer = staminaRegenDelay; // сбрасываем задержку перед восстановлением
        }
        else
        {
            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        if (StaminaHUD.Instance != null)
        {
            StaminaHUD.Instance.SetStamina(currentStamina / maxStamina);
        }
    }

    void HandleMove()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // прижимает к земле, чтобы isGrounded корректно работал

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float speed;
        if (isCrouching)
            speed = crouchSpeed; // присед всегда медленный, спринт при этом игнорируется
        else if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f)
            speed = sprintSpeed;
        else
            speed = walkSpeed;

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        Generator hitGenerator = null;

        if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
        {
            hitGenerator = hit.collider.GetComponent<Generator>();
        }

        // если то, на что мы смотрим, изменилось — обновляем подсказки
        if (hitGenerator != currentGenerator)
        {
            if (currentGenerator != null)
            {
                currentGenerator.HidePrompt();
                currentGenerator.StopRepair(); // если чинили и отвели взгляд — прерываем ремонт
            }

            if (hitGenerator != null)
            {
                hitGenerator.ShowPrompt();
            }

            currentGenerator = hitGenerator;
        }

        if (currentGenerator != null)
        {
            if (Input.GetKeyDown(interactKey))
            {
                currentGenerator.StartRepair();
            }
            if (Input.GetKeyUp(interactKey))
            {
                currentGenerator.StopRepair();
            }
        }
    }
}