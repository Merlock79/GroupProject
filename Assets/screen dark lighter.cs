using UnityEngine;

public class DarknessManager : MonoBehaviour
{
    public static DarknessManager Instance;

    public DarknessOverlay overlay;
    public PlayerVision vision;

    [Header("Default Settings")]
    public float defaultDarkness = 0.8f;
    public float defaultVision = 3f;

    private float currentDarkness;
    private float currentVision;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentDarkness = defaultDarkness;
        currentVision = defaultVision;

        Apply();
    }

    public void SetZone(float darkness, float visionDistance)
    {
        currentDarkness = darkness;
        currentVision = visionDistance;
        Apply();
    }

    public void ClearZone()
    {
        currentDarkness = defaultDarkness;
        currentVision = defaultVision;
        Apply();
    }

    public void Apply()
    {
        overlay.SetDarkness(currentDarkness);
        vision.visionDistance = currentVision;
    }

    public void AddLight(float strength)
    {
        currentDarkness -= strength;
        currentVision += strength * 2f;
        Apply();
    }

    public void RemoveLight(float strength)
    {
        currentDarkness += strength;
        currentVision -= strength * 2f;
        Apply();
    }
}
