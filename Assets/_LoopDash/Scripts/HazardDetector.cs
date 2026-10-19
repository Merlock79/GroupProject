using System;
using UnityEngine;

public class HazardDetector : MonoBehaviour
{
    public const string HazardTag = "Hazard";

    public event Action<GameObject> OnHazardDetected;

    private void OnCollisionEnter(Collision collision)
    {
        Check(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        Check(other);
    }

    private void Check(Collider other)
    {
        if (other.CompareTag(HazardTag))
        {
            OnHazardDetected?.Invoke(other.gameObject);
        }
    }
}