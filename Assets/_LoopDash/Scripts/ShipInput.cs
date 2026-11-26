using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipController), typeof(ShipWeapon))]
public class ShipInput : MonoBehaviour
{
    private ShipController controller;
    private ShipWeapon weapon;
    private InputAction spaceAction;
    private InputAction mouseAction;
    private InputAction shootAction;
    private bool spaceHeld;
    private bool mouseHeld;

    private void Awake()
    {
        controller = GetComponent<ShipController>();
        weapon = GetComponent<ShipWeapon>();
        spaceAction = new InputAction("Ship Thrust Space", InputActionType.Button, "<Keyboard>/space");
        mouseAction = new InputAction("Ship Thrust Mouse", InputActionType.Button, "<Mouse>/leftButton");
        shootAction = new InputAction("Ship Shoot", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        spaceAction.performed += OnSpace;
        spaceAction.canceled += OnSpaceReleased;
        mouseAction.performed += OnMouse;
        mouseAction.canceled += OnMouseReleased;
        spaceAction.Enable();
        mouseAction.Enable();
        shootAction.Enable();
    }

    private void OnDisable()
    {
        spaceAction.performed -= OnSpace;
        spaceAction.canceled -= OnSpaceReleased;
        mouseAction.performed -= OnMouse;
        mouseAction.canceled -= OnMouseReleased;
        spaceAction.Disable();
        mouseAction.Disable();
        shootAction.Disable();
        spaceHeld = mouseHeld = false;
        controller.SetThrust(false);
    }

    private void LateUpdate()
    {
        if (shootAction.IsPressed())
            weapon.Shoot();
    }

    private void OnDestroy()
    {
        spaceAction.Dispose();
        mouseAction.Dispose();
        shootAction.Dispose();
    }

    private void OnSpace(InputAction.CallbackContext _) { spaceHeld = true; UpdateThrust(); }
    private void OnSpaceReleased(InputAction.CallbackContext _) { spaceHeld = false; UpdateThrust(); }
    private void OnMouse(InputAction.CallbackContext _) { mouseHeld = true; UpdateThrust(); }
    private void OnMouseReleased(InputAction.CallbackContext _) { mouseHeld = false; UpdateThrust(); }
    private void UpdateThrust() => controller.SetThrust(spaceHeld || mouseHeld);
}
