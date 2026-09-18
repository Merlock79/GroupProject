
using UnityEngine;

public class MovementController : MonoBehaviour
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
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
        ApplyGravity();
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // Sprint
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
            currentSpeed = sprintSpeed;
        else if (isCrouching)
            currentSpeed = crouchSpeed;
        else
            currentSpeed = walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            isCrouching = !isCrouching;

        float targetHeight = isCrouching ? crouchHeight : normalHeight;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * heightSmooth);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
