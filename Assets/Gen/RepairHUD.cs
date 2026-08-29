using UnityEngine;
using UnityEngine.UI;

public class RepairHUD : MonoBehaviour
{
    public static RepairHUD Instance { get; private set; }

    [Header("Ссылки на UI")]
    public GameObject panel;
    public Slider progressSlider;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void SetProgress(float value01)
    {
        if (progressSlider != null) progressSlider.value = value01;
    }
}
