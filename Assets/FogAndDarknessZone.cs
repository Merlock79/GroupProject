using UnityEngine;
using UnityEngine.UI;

public class DarknessOverlay : MonoBehaviour
{
    [Range(0f, 1f)]
    public float darknessStrength = 0.8f; // how dark the screen is

    private Image img;

    void Start()
    {
        img = GetComponent<Image>();
    }

    public void SetDarkness(float value)
    {
        Color c = img.color;
        c.a = Mathf.Clamp01(value);
        img.color = c;
    }
}
