using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipController))]
public class ShipInput : MonoBehaviour
{
    private ShipController controller;
    private InputAction spaceAction;
    private InputAction mouseAction;
    private bool spaceHeld;
    private bool mouseHeld;

    private void Awake()
    {
        controller = GetComponent<ShipController>();
        spaceAction = new InputAction("Ship Thrust Space", InputActionType.Button, "<Keyboard>/space");
        mouseAction = new InputAction("Ship Thrust Mouse", InputActionType.Button, "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        spaceAction.performed += OnSpace;
        spaceAction.canceled += OnSpaceReleased;
        mouseAction.performed += OnMouse;
        mouseAction.canceled += OnMouseReleased;
        spaceAction.Enable();
        mouseAction.Enable();
    }

    private void OnDisable()
    {
        spaceAction.performed -= OnSpace;
        spaceAction.canceled -= OnSpaceReleased;
        mouseAction.performed -= OnMouse;
        mouseAction.canceled -= OnMouseReleased;
        spaceAction.Disable();
        mouseAction.Disable();
        spaceHeld = mouseHeld = false;
        controller.SetThrust(false);
    }

    private void OnSpace(InputAction.CallbackContext _) { spaceHeld = true; UpdateThrust(); }
    private void OnSpaceReleased(InputAction.CallbackContext _) { spaceHeld = false; UpdateThrust(); }
    private void OnMouse(InputAction.CallbackContext _) { mouseHeld = true; UpdateThrust(); }
    private void OnMouseReleased(InputAction.CallbackContext _) { mouseHeld = false; UpdateThrust(); }
    private void UpdateThrust() => controller.SetThrust(spaceHeld || mouseHeld);
}
