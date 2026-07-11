using UnityEngine;
using UnityEngine.InputSystem;

public class FlowerWatering : MonoBehaviour
{
    [SerializeField] private float wateringDistance = 4f;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, wateringDistance))
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
