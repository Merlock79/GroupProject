using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    public Transform player;
    public float visionDistance = 3f; // how far player can see

    private RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Follow player position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(player.position);
        rt.position = screenPos;

        // Scale mask based on vision distance
        float size = visionDistance * 200f;
        rt.sizeDelta = new Vector2(size, size);
    }
}
