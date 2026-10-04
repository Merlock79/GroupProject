using UnityEngine;

public class MovementControllerOld : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 2f;

    [Header("Crouch Settings")]
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    public float heightSmooth = 8f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isCrouching;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
    }

    private void Update()
    {
        HandleMovement();
        HandleCrouch();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
            currentSpeed = sprintSpeed;
        else if (isCrouching)
            currentSpeed = crouchSpeed;
        else
            currentSpeed = walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            isCrouching = !isCrouching;

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * heightSmooth);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
