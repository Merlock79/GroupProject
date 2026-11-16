using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CubeMovement))]
[RequireComponent(typeof(CubeExplosion))]
public class CubeInput : MonoBehaviour
{
    private CubeMovement movement;
    private CubeExplosion explosion;

    private InputAction spaceJumpAction;
    private InputAction mouseJumpAction;
    private InputAction explodeAction;
    private bool spaceJumpHeld;
    private bool mouseJumpHeld;

    private void Awake()
    {
        movement = GetComponent<CubeMovement>();
        explosion = GetComponent<CubeExplosion>();

        spaceJumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        mouseJumpAction = new InputAction("Jump", InputActionType.Button, "<Mouse>/leftButton");
        explodeAction = new InputAction("Explode", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        spaceJumpAction.performed += OnSpaceJumpPressed;
        spaceJumpAction.canceled += OnSpaceJumpReleased;
        mouseJumpAction.performed += OnMouseJumpPressed;
        mouseJumpAction.canceled += OnMouseJumpReleased;
        explodeAction.performed += OnExplode;
        spaceJumpAction.Enable();
        mouseJumpAction.Enable();
        explodeAction.Enable();
    }

    private void OnDisable()
    {
        spaceJumpAction.performed -= OnSpaceJumpPressed;
        spaceJumpAction.canceled -= OnSpaceJumpReleased;
        mouseJumpAction.performed -= OnMouseJumpPressed;
        mouseJumpAction.canceled -= OnMouseJumpReleased;
        explodeAction.performed -= OnExplode;
        spaceJumpHeld = false;
        mouseJumpHeld = false;
        movement.SetJumpHeld(false);
        spaceJumpAction.Disable();
        mouseJumpAction.Disable();
        explodeAction.Disable();
    }

    private void OnSpaceJumpPressed(InputAction.CallbackContext ctx)
    {
        bool anotherJumpButtonHeld = mouseJumpHeld;
        spaceJumpHeld = true;
        if (anotherJumpButtonHeld) movement.Jump();
        else movement.SetJumpHeld(true);
    }

    private void OnMouseJumpPressed(InputAction.CallbackContext ctx)
    {
        bool anotherJumpButtonHeld = spaceJumpHeld;
        mouseJumpHeld = true;
        if (anotherJumpButtonHeld) movement.Jump();
        else movement.SetJumpHeld(true);
    }

    private void OnSpaceJumpReleased(InputAction.CallbackContext ctx)
    {
        spaceJumpHeld = false;
        movement.SetJumpHeld(mouseJumpHeld);
    }

    private void OnMouseJumpReleased(InputAction.CallbackContext ctx)
    {
        mouseJumpHeld = false;
        movement.SetJumpHeld(spaceJumpHeld);
    }

    private void OnExplode(InputAction.CallbackContext ctx) => explosion.Explode();
}
