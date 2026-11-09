using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CubeMovement))]
[RequireComponent(typeof(CubeExplosion))]
public class CubeInput : MonoBehaviour
{
    private CubeMovement movement;
    private CubeExplosion explosion;

    private InputAction jumpAction;
    private InputAction explodeAction;

    private void Awake()
    {
        movement = GetComponent<CubeMovement>();
        explosion = GetComponent<CubeExplosion>();

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.AddBinding("<Mouse>/leftButton");
        jumpAction.wantsInitialStateCheck = true;
        explodeAction = new InputAction("Explode", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPressed;
        jumpAction.canceled += OnJumpReleased;
        explodeAction.performed += OnExplode;
        jumpAction.Enable();
        explodeAction.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPressed;
        jumpAction.canceled -= OnJumpReleased;
        explodeAction.performed -= OnExplode;
        movement.SetJumpHeld(false);
        jumpAction.Disable();
        explodeAction.Disable();
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx) => movement.SetJumpHeld(true);

    private void OnJumpReleased(InputAction.CallbackContext ctx) => movement.SetJumpHeld(false);

    private void OnExplode(InputAction.CallbackContext ctx) => explosion.Explode();
}
