using UnityEngine;

[RequireComponent(typeof(ShipMovement))]
public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipMovement movement;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<ShipMovement>();
    }

    public void SetThrust(bool held)
    {
        if (movement != null) movement.SetThrust(held);
    }
}
