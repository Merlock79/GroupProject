using System;
using UnityEngine;

internal sealed class CubeCollision
{
    internal const float ContactTolerance = 0.0005f;
    private const float CastInset = 0.003f;

    private readonly PhysicsScene scene;
    private readonly BoxCollider player;
    private readonly Rigidbody body;
    private readonly int collisionMask;
    private readonly Vector3 centerOffset;
    private readonly Vector3 halfExtents;
    private readonly Vector3 castHalfExtents;
    private RaycastHit[] hits = new RaycastHit[8];
    private Collider[] overlaps = new Collider[8];

    internal CubeCollision(PhysicsScene scene, BoxCollider player, int collisionMask)
    {
        this.scene = scene;
        this.player = player;
        body = player.attachedRigidbody;
        this.collisionMask = collisionMask;

        Bounds bounds = player.bounds;
        centerOffset = bounds.center - body.position;
        halfExtents = bounds.extents;
        castHalfExtents = Vector3.Max(halfExtents - Vector3.one * CastInset, halfExtents * 0.5f);
    }

    internal static bool IsFloor(RaycastHit hit) => hit.normal.y >= 0.5f;

    internal bool TryGetFloor(Vector3 position, float distance, out float drop)
    {
        drop = DistanceToSolid(position, Vector3.down, distance, out RaycastHit hit);
        return hit.collider != null && IsFloor(hit);
    }

    internal bool TryGetStep(Vector3 position, Vector3 direction, float distance, float stepHeight,
                             out float lift, out float drop)
    {
        lift = 0f;
        drop = 0f;
        if (!TryGetFloor(position, ContactTolerance, out _)) return false;

        lift = DistanceToSolid(position, Vector3.up, stepHeight, out _);
        if (lift <= ContactTolerance) return false;

        Vector3 raisedPosition = position + Vector3.up * lift;
        if (DistanceToSolid(raisedPosition, direction, distance, out _) < distance) return false;

        Vector3 destination = raisedPosition + direction * distance;
        return TryGetFloor(destination, lift, out drop) && lift - drop > ContactTolerance;
    }

    internal float DistanceToSolid(Vector3 position, Vector3 direction, float distance, out RaycastHit closest)
    {
        // Keep the sides of the sweep off flush surfaces, then restore its leading face.
        Vector3 inset = halfExtents - castHalfExtents;
        float leadingInset = Mathf.Abs(Vector3.Dot(inset, direction));
        float closestDistance = distance + leadingInset;
        int count = Cast(position, castHalfExtents, direction, closestDistance, QueryTriggerInteraction.Ignore);
        closest = default;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            Collider other = hit.collider;
            if (other.attachedRigidbody == body || hit.distance > closestDistance) continue;

            if (hit.distance == 0f && hit.point == Vector3.zero)
            {
                // A sweep that starts overlapped reports the reverse cast direction as its normal.
                if (!Physics.ComputePenetration(player, position, body.rotation,
                        other, other.transform.position, other.transform.rotation, out Vector3 normal, out _))
                {
                    continue;
                }

                hit.normal = normal;
            }

            if (Vector3.Dot(hit.normal, direction) >= -ContactTolerance) continue;
            if (hit.distance == closestDistance && closest.collider != null && closest.normal.y >= hit.normal.y) continue;

            closest = hit;
            closestDistance = hit.distance;
        }

        return closest.collider != null ? Mathf.Clamp(closestDistance - leadingInset, 0f, distance) : distance;
    }

    internal Collider FindOverlappingHazard(Vector3 position)
    {
        int count = scene.OverlapBox(position + centerOffset, halfExtents, overlaps, Quaternion.identity,
                                     collisionMask, QueryTriggerInteraction.Collide);
        while (count == overlaps.Length)
        {
            Array.Resize(ref overlaps, overlaps.Length * 2);
            count = scene.OverlapBox(position + centerOffset, halfExtents, overlaps, Quaternion.identity,
                                     collisionMask, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            Collider other = overlaps[i];
            if (other.attachedRigidbody != body && other.CompareTag(HazardDetector.HazardTag)) return other;
        }

        return null;
    }

    internal Collider FindHazardAlong(Vector3 position, Vector3 direction, float distance, out float travel)
    {
        int count = Cast(position, halfExtents, direction, distance + ContactTolerance, QueryTriggerInteraction.Collide);
        Collider hazard = null;
        float nearestDistance = distance + ContactTolerance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            Collider other = hit.collider;
            if (hit.distance > nearestDistance || other.attachedRigidbody == body || !other.CompareTag(HazardDetector.HazardTag))
            {
                continue;
            }

            hazard = other;
            nearestDistance = hit.distance;
        }

        travel = Mathf.Min(nearestDistance, distance);
        return hazard;
    }

    private int Cast(Vector3 position, Vector3 extents, Vector3 direction, float distance, QueryTriggerInteraction triggers)
    {
        int count = scene.BoxCast(position + centerOffset, extents, direction, hits, Quaternion.identity,
                                  distance, collisionMask, triggers);
        while (count == hits.Length)
        {
            Array.Resize(ref hits, hits.Length * 2);
            count = scene.BoxCast(position + centerOffset, extents, direction, hits, Quaternion.identity,
                                  distance, collisionMask, triggers);
        }

        return count;
    }
}
