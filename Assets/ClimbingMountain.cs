using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockClimber : MonoBehaviour
{
    [Header("Rock Parents")]
    public List<Transform> rockParents;   // drag your 2 empty objects here

    [Header("Settings")]
    public float detectDistance = 4f;
    public float jumpDistance = 6f;
    public Camera cam;

    Transform currentRock = null;
    bool climbing = false;

    void Update()
    {
        if (!climbing)
        {
            HighlightClosestRock();

            if (Input.GetKeyDown(KeyCode.R))
                AttachToClosestRock();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
                DropFromRock();

            if (Input.GetKeyDown(KeyCode.Space))
                JumpToNextRock();
        }
    }

    // ---------------- HIGHLIGHT ----------------
    void HighlightClosestRock()
    {
        Transform closest = GetClosestRock();

        foreach (Transform parent in rockParents)
        {
            foreach (Transform r in parent)
            {
                var rock = r.GetComponent<ClimbRock>();
                if (rock != null)
                    rock.Highlight(r == closest);
            }
        }
    }

    // ---------------- ATTACH ----------------
    void AttachToClosestRock()
    {
        Transform closest = GetClosestRock();
        if (closest == null) return;

        currentRock = closest;
        climbing = true;

        transform.position = closest.position;
        transform.SetParent(closest);

        CameraShake();
    }

    // ---------------- DROP ----------------
    void DropFromRock()
    {
        climbing = false;
        transform.SetParent(null);
        currentRock = null;
    }

    // ---------------- JUMP ----------------
    void JumpToNextRock()
    {
        if (currentRock == null) return;

        Vector3 dir = GetInputDirection();
        if (dir == Vector3.zero) return;

        Transform next = GetClosestRockInDirection(dir);
        if (next == null) return;

        currentRock = next;
        transform.SetParent(next);
        transform.position = next.position;

        CameraShake();
    }

    // ---------------- HELPERS ----------------
    Transform GetClosestRock()
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (Transform parent in rockParents)
        {
            foreach (Transform r in parent)
            {
                float d = Vector3.Distance(transform.position, r.position);
                if (d < bestDist && d < detectDistance)
                {
                    bestDist = d;
                    best = r;
                }
            }
        }
        return best;
    }

    Transform GetClosestRockInDirection(Vector3 dir)
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (Transform parent in rockParents)
        {
            foreach (Transform r in parent)
            {
                Vector3 toRock = r.position - transform.position;
                float angle = Vector3.Angle(dir, toRock);

                if (angle < 60f)
                {
                    float d = toRock.magnitude;
                    if (d < bestDist && d < jumpDistance)
                    {
                        bestDist = d;
                        best = r;
                    }
                }
            }
        }
        return best;
    }

    Vector3 GetInputDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        return new Vector3(h, 0, v).normalized;
    }

    // ---------------- CAMERA SHAKE ----------------
    void CameraShake()
    {
        if (cam == null) return;
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float duration = 0.15f;
        float strength = 0.2f;

        Vector3 original = cam.transform.localPosition;

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cam.transform.localPosition = original + Random.insideUnitSphere * strength;
            yield return null;
        }

        cam.transform.localPosition = original;
    }
}