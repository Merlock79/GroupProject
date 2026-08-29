using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        FindCamera();
    }

    void FindCamera()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        else
        {
            Camera anyCam = FindObjectOfType<Camera>();
            if (anyCam != null) cam = anyCam.transform;
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            FindCamera();
            if (cam == null) return;
        }

        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }
}