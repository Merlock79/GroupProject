using UnityEngine;

public class PlayerLightSource : MonoBehaviour
{
    public float lightStrength = 0.3f; // how much darkness it removes
    public bool isOn = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleLight();
        }
    }

    void ToggleLight()
    {
        isOn = !isOn;

        if (isOn)
        {
            DarknessManager.Instance.AddLight(lightStrength);
        }
        else
        {
            DarknessManager.Instance.RemoveLight(lightStrength);
        }
    }
}