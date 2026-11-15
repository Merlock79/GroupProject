using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the active level's horizontal completion percentage.</summary>
public class LevelProgressUI : MonoBehaviour
{
    [SerializeField] private Level level;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentageLabel;

    private void OnEnable()
    {
        if (level == null) level = FindAnyObjectByType<Level>();
        ResolveVisuals();
        if (fillImage != null)
        {
            fillImage.fillAmount = level.Progress / 100f;

		}
        if (percentageLabel != null) percentageLabel.text = "0%";
        Refresh();
    }

    private void Update()
    {
        if (level == null) level = FindAnyObjectByType<Level>();
        Refresh();
    }

    private void Refresh()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = level.Progress / 100f;
        }
        if (percentageLabel != null)
        {
			float progress = level != null ? level.Progress : 0f;
			percentageLabel.text = $"{Mathf.RoundToInt(progress)}%";
        }
    }

    private void ResolveVisuals()
    {
        if (fillImage == null)
        {
            Transform fill = transform.Find("Track/Fill");
            if (fill != null) fillImage = fill.GetComponent<Image>();
        }

        if (percentageLabel == null)
        {
            Transform label = transform.Find("Percentage");
            if (label != null) percentageLabel = label.GetComponent<TMP_Text>();
        }
    }
}
