using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class JumpPad : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Vertical height of the launch arc, in world units.")]
    private float jumpHeight = 5.5f;

    private readonly Dictionary<CubeMovement, HashSet<Collider>> activeContacts =
        new Dictionary<CubeMovement, HashSet<Collider>>();
    private readonly HashSet<CubeMovement> launchedSinceLanding = new HashSet<CubeMovement>();
    private readonly List<CubeMovement> contactsToRelease = new List<CubeMovement>();

    public float JumpHeight => jumpHeight;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CubeMovement movement = other.GetComponentInParent<CubeMovement>();
        if (movement == null) return;

        if (!activeContacts.TryGetValue(movement, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            activeContacts.Add(movement, colliders);
        }

        if (!colliders.Add(other)) return;

        // A player can have more than one collider. Launch once until all of them leave.
        if (colliders.Count == 1 && launchedSinceLanding.Add(movement))
        {
            movement.ApplyJump(jumpHeight);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CubeMovement movement = other.GetComponentInParent<CubeMovement>();
        if (movement == null || !activeContacts.TryGetValue(movement, out HashSet<Collider> colliders)) return;

        colliders.Remove(other);
        if (colliders.Count == 0)
        {
            activeContacts.Remove(movement);
        }
    }

    private void OnDisable()
    {
        activeContacts.Clear();
        launchedSinceLanding.Clear();
        contactsToRelease.Clear();
    }

    private void Update()
    {
        if (launchedSinceLanding.Count == 0) return;

        contactsToRelease.Clear();
        foreach (CubeMovement movement in launchedSinceLanding)
        {
            if (movement == null || (movement.IsGrounded() && !activeContacts.ContainsKey(movement)))
            {
                contactsToRelease.Add(movement);
            }
        }

        foreach (CubeMovement movement in contactsToRelease)
        {
            launchedSinceLanding.Remove(movement);
        }
    }
}
