
using UnityEngine;

public class TerrainSlopeSlip : MonoBehaviour
{
    public CharacterController controller;

    [Header("Slip Settings")]
    public float slipStartAngle = 35f;   // start slipping
    public float slipStopAngle = 25f;    // stop slipping
    public float slipForce = 6f;         // how fast you slide

    private Vector3 slipDirection;

    void Update()
    {
        CheckSlope();
        ApplySlip();
    }

    void CheckSlope()
    {
        RaycastHit hit;

        // Cast ray downward to detect terrain
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 3f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle > slipStartAngle)
            {
                // Calculate slide direction
                slipDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
            }
            else if (slopeAngle < slipStopAngle)
            {
                // Stop slipping
                slipDirection = Vector3.zero;
            }
        }
    }

    void ApplySlip()
    {
        if (slipDirection != Vector3.zero)
        {
            controller.Move(slipDirection * slipForce * Time.deltaTime);
        }
    }
}
