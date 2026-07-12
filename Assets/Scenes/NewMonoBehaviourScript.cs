using UnityEngine;

public class PlayerWatering : MonoBehaviour
{
    public float distance = 5f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, distance))
            {
                MagicFlower flower = hit.collider.GetComponent<MagicFlower>();
                if (flower != null)
                {
                    flower.Water();
                }
            }
        }
    }
}
