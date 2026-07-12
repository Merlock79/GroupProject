using UnityEngine;

public class ColorChangingFlower : MagicFlower
{
    public Color newColor = Color.magenta;
    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public override void Water()
    {
        if (rend != null)
        {
            rend.material.color = newColor;
            Debug.Log("Квітка змінила колір!");
        }
    }
}
