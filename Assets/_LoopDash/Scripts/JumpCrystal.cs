using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpCrystal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private Collider pickupCollider;
    private bool collected;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        CubeMovement movement = other.GetComponentInParent<CubeMovement>();
        if (movement == null || !movement.CompareTag(playerTag)) return;

        collected = true;
        pickupCollider.enabled = false;
        movement.TryAddExtraJump();
        Destroy(gameObject);
    }
}
