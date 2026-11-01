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
        explodeAction = new InputAction("Explode", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJump;
        explodeAction.performed += OnExplode;
        jumpAction.Enable();
        explodeAction.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        explodeAction.performed -= OnExplode;
        jumpAction.Disable();
        explodeAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx) => movement.Jump();

    private void OnExplode(InputAction.CallbackContext ctx) => explosion.Explode();
}