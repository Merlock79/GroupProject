using UnityEngine;

public class DarknessZone : MonoBehaviour
{
    public float zoneDarkness = 0.8f;     // how dark this zone is
    public float zoneVision = 3f;         // how far player can see in this zone

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DarknessManager.Instance.SetZone(zoneDarkness, zoneVision);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DarknessManager.Instance.ClearZone();
        }
    }
}
