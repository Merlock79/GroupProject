using UnityEngine;

public class ClimbRock : MonoBehaviour
{
    Renderer rend;
    Color baseColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        baseColor = rend.material.color;
    }

    public void Highlight(bool on)
    {
        rend.material.color = on ? Color.white : baseColor;
    }
}