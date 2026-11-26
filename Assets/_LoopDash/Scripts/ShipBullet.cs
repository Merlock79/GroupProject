using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class ShipBullet : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 25f;
    [SerializeField, Min(0.1f)] private float lifetime = 4f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

    private Rigidbody body;
    private SphereCollider sphere;
    private PhysicsScene physicsScene;
    private Transform owner;
    private Vector3 direction;
    private float radius;
    private bool spent;
    private RaycastHit[] hits = new RaycastHit[8];
    private Collider[] overlaps = new Collider[8];

    public float Damage { get; private set; } = 1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
        physicsScene = gameObject.scene.GetPhysicsScene();
        Vector3 scale = transform.lossyScale;
        radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
    }

    public void Initialize(Vector3 flightDirection, float damage, Transform shooter)
    {
        direction = flightDirection.normalized;
        transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        Damage = damage;
        owner = shooter;
        Destroy(gameObject, lifetime);

        // A sweep can miss a collider that already overlaps the muzzle at spawn.
        int count = physicsScene.OverlapSphere(body.position, radius, overlaps, collisionMask, QueryTriggerInteraction.Ignore);
        while (count == overlaps.Length)
        {
            Array.Resize(ref overlaps, overlaps.Length * 2);
            count = physicsScene.OverlapSphere(body.position, radius, overlaps, collisionMask, QueryTriggerInteraction.Ignore);
        }

        for (int i = 0; i < count; i++)
        {
            if (IsTarget(overlaps[i]))
            {
                Hit(overlaps[i]);
                break;
            }
        }
    }

    private void FixedUpdate()
    {
        if (spent)
            return;

        float distance = speed * Time.fixedDeltaTime;
        int count = physicsScene.SphereCast(body.position, radius, direction, hits, distance, collisionMask, QueryTriggerInteraction.Ignore);
        while (count == hits.Length)
        {
            Array.Resize(ref hits, hits.Length * 2);
            count = physicsScene.SphereCast(body.position, radius, direction, hits, distance, collisionMask, QueryTriggerInteraction.Ignore);
        }

        Collider nearest = null;
        float nearestDistance = distance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance <= nearestDistance && IsTarget(hit.collider))
            {
                nearest = hit.collider;
                nearestDistance = hit.distance;
            }
        }

        if (nearest != null)
            Hit(nearest);
        else
            body.MovePosition(body.position + direction * distance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!spent && IsTarget(other))
            Hit(other);
    }

    private bool IsTarget(Collider other)
    {
        return !other.isTrigger && other != sphere
            && (collisionMask.value & (1 << other.gameObject.layer)) != 0
            && (owner == null || !other.transform.IsChildOf(owner));
    }

    private void Hit(Collider other)
    {
        spent = true;
        sphere.enabled = false;
        DamageableBlock block = other.GetComponentInParent<DamageableBlock>();
        if (block != null)
            block.TakeDamage(Damage);

        Destroy(gameObject);
    }
}
