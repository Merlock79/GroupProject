using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CubeMovement))]
public class CubeInput : MonoBehaviour
{
    private CubeMovement movement;
    private InputAction jumpAction;

    private void Awake()
    {
        movement = GetComponent<CubeMovement>();
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJump;
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        jumpAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        movement.Jump();
    }
}