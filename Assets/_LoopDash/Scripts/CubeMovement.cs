using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeMovement : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField] private float speed = 4.5f;

    [Header("Стрибок")]
    [SerializeField] private float jumpVelocity = 6.8f;
    [SerializeField] private LayerMask groundMask;

    [Header("Перевірка землі")]
    [SerializeField] private float checkDepth = 0.06f;
    [SerializeField] private float checkWidth = 0.45f;

    private Rigidbody rb;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Jump()
    {
        jumpRequested = true;
    }

    public bool IsGrounded()
    {
        Vector3 center = rb.position + Vector3.down * 0.5f;
        Vector3 halfExtents = new Vector3(checkWidth, checkDepth, checkWidth);
        return Physics.CheckBox(center, halfExtents, Quaternion.identity,
                                groundMask, QueryTriggerInteraction.Ignore);
    }

    private void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;
        v.x = speed;
        v.z = 0f;

        if (jumpRequested && v.y <= 0.1f && IsGrounded())
        {
            v.y = jumpVelocity;
        }
        jumpRequested = false;

        rb.linearVelocity = v;
    }
}