using System;
using UnityEngine;

public class HazardDetector : MonoBehaviour
{
    public const string HazardTag = "Hazard";

    public event Action<GameObject> OnHazardDetected;

    private void OnCollisionEnter(Collision collision)
    {
        ReportContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ReportContact(other);
    }

    internal void ReportContact(Collider other)
    {
        if (other.CompareTag(HazardTag))
        {
            ReportFatalContact(other);
        }
    }

    public void ReportFatalContact(Collider other)
    {
        if (other != null) OnHazardDetected?.Invoke(other.gameObject);
    }
}
