using UnityEngine;
using UnityEngine.UI;

public class StaminaHUD : MonoBehaviour
{
    public static StaminaHUD Instance { get; private set; }

    [Header("Ссылки на UI")]
    public GameObject panel; // родительский объект бара стамины
    public Slider staminaSlider;

    [Header("Настройки")]
    [Tooltip("Если включено - бар прячется, когда стамина на 100%, и появляется при первом же расходе")]
    public bool hideWhenFull = true;

    void Awake()
    {
        Instance = this;
    }

    public void SetStamina(float value01)
    {
        if (staminaSlider != null) staminaSlider.value = value01;

        if (panel != null && hideWhenFull)
        {
            panel.SetActive(value01 < 0.999f);
        }
    }
}
