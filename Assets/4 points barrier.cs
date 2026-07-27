using UnityEngine;

/// <summary>
/// Creates four wall segments between the provided corner points.
/// Walls are created as visible cube primitives (so you can see them in-game)
/// and use BoxColliders to block movement in the physics system.
///
/// Usage:
/// - Assign the four corner Transforms in order (A→B→C→D).
/// - Set wallHeight / wallThickness and choose a layer or tag-based blocking.
/// - If you want only a specific tag to be blocked, enable Only Block Tag.
///   (This uses Physics.IgnoreCollision on first contact for non-matching objects.)
/// </summary>
[DisallowMultipleComponent]
public class SquareBarrier : MonoBehaviour
{
    [Header("Corners (in order)")]
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;
    public Transform pointD;

    [Header("Wall Appearance")]
    [Tooltip("If true walls are created as visible cube primitives. If false walls are invisible colliders.")]
    public bool createVisibleWalls = true;
    [Tooltip("Optional material used on visible walls.")]
    public Material wallMaterial;
    [Tooltip("Optional layer to assign created walls to (use LayerMask inspector to pick single layer).")]
    public LayerMask wallLayer;

    [Header("Wall Geometry")]
    public float wallHeight = 3f;
    public float wallThickness = 0.2f;

    [Header("Blocking Behaviour")]
    [Tooltip("If true, only objects with Block Tag will remain blocked. Others will be allowed through after first contact.")]
    public bool onlyBlockTag = false;
    [Tooltip("Tag of objects that should be blocked when 'Only Block Tag' is enabled.")]
    public string blockTag = "Player";

    void Start()
    {
        // Validate corners before attempting to create walls
        if (pointA == null || pointB == null || pointC == null || pointD == null)
        {
            Debug.LogWarning("SquareBarrier: One or more corner points are not assigned. Barrier will not be created.", this);
            return;
        }

        CreateWall(pointA, pointB);
        CreateWall(pointB, pointC);
        CreateWall(pointC, pointD);
        CreateWall(pointD, pointA);
    }

    void CreateWall(Transform start, Transform end)
    {
        if (start == null || end == null) return;

        // Create a cube primitive so we get MeshFilter, MeshRenderer and BoxCollider set up correctly.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BarrierWall";
        wall.transform.SetParent(transform, true);

        // Horizontal direction between points (ignore vertical difference)
        Vector3 dir = end.position - start.position;
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        float length = flatDir.magnitude;
        if (length <= 0.0001f)
        {
            Debug.LogWarning("SquareBarrier: zero-length wall between two identical corner positions.", this);
            DestroyImmediate(wall);
            return;
        }

        // Position & rotation
        Vector3 midPoint = (start.position + end.position) * 0.5f;
        midPoint.y += wallHeight * 0.5f;
        wall.transform.position = midPoint;
        wall.transform.rotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

        // Scale: cube's forward (local Z) is used for length
        wall.transform.localScale = new Vector3(wallThickness, wallHeight, length);

        // Configure renderer & material
        var renderer = wall.GetComponent<MeshRenderer>();
        if (createVisibleWalls)
        {
            if (wallMaterial != null)
                renderer.sharedMaterial = wallMaterial;
        }
        else
        {
            // Hide renderer if not desired visible
            if (renderer != null)
                renderer.enabled = false;
        }

        // Configure collider
        var collider = wall.GetComponent<BoxCollider>();
        if (collider == null)
            collider = wall.AddComponent<BoxCollider>();

        collider.isTrigger = false; // solid blocking by default

        // Assign layer if specified (pick the first bit of the mask)
        int layer = MaskToSingleLayer(wallLayer);
        if (layer >= 0)
            wall.layer = layer;

        // Make the wall static for physics/performance
        wall.isStatic = true;

        // If tag-based selective blocking is requested, attach helper
        if (onlyBlockTag)
        {
            var bw = wall.AddComponent<BarrierWall>();
            bw.blockTag = blockTag;
            bw.Initialize(collider);
        }
        else
        {
            // Ensure default tag/layer behaviour blocks all objects via the solid collider.
            // No extra component required.
            var bw = wall.GetComponent<BarrierWall>();
            if (bw != null)
                Destroy(bw);
        }
    }

    // Convert LayerMask to a single layer index; returns -1 if multiple/no layers selected.
    int MaskToSingleLayer(LayerMask mask)
    {
        int m = mask.value;
        if (m == 0) return -1; // none
        // If more than one bit set, return -1
        if ((m & (m - 1)) != 0) return -1;
        int layer = 0;
        while ((m >>= 1) > 0) layer++;
        return layer;
    }

    void OnDrawGizmos()
    {
        if (pointA == null || pointB == null || pointC == null || pointD == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawLine(pointB.position, pointC.position);
        Gizmos.DrawLine(pointC.position, pointD.position);
        Gizmos.DrawLine(pointD.position, pointA.position);
    }
}

/// <summary>
/// Helper attached to walls when selective tag-based blocking is desired.
/// Behavior:
/// - If an object that does NOT match blockTag collides, the wall will temporarily ignore collisions with that collider.
/// - When that object exits collision, collisions are restored.
/// Notes:
/// - This uses Physics.IgnoreCollision between the wall collider and the other collider.
/// - It is a simple fallback for cases where you cannot (or do not want to) use layers/collision matrix.
/// </summary>
public class BarrierWall : MonoBehaviour
{
    [Tooltip("Tag that should be blocked. Other tags will be allowed through.")]
    public string blockTag = "Player";

    private Collider myCollider;

    internal void Initialize(Collider c)
    {
        myCollider = c ?? GetComponent<Collider>();
        // ensure collider exists and is not a trigger
        if (myCollider != null)
            myCollider.isTrigger = false;
    }

    void Awake()
    {
        if (myCollider == null)
            myCollider = GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision other)
    {
        if (myCollider == null || other == null) return;

        // If the other object DOESN'T have the block tag, ignore collision with its collider so it can pass through.
        if (!other.gameObject.CompareTag(blockTag))
        {
            // Ignore this specific collider; will be restored on exit
            Physics.IgnoreCollision(myCollider, other.collider, true);
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (myCollider == null || other == null) return;

        // Restore collision when the other leaves
        if (!other.gameObject.CompareTag(blockTag))
        {
            Physics.IgnoreCollision(myCollider, other.collider, false);
        }
    }

    // Safety: if a trigger is used accidentally, treat similarly (best-effort)
    void OnTriggerEnter(Collider other)
    {
        if (myCollider == null || other == null) return;
        if (!other.CompareTag(blockTag))
            Physics.IgnoreCollision(myCollider, other, true);
    }

    void OnTriggerExit(Collider other)
    {
        if (myCollider == null || other == null) return;
        if (!other.CompareTag(blockTag))
            Physics.IgnoreCollision(myCollider, other, false);
    }
}