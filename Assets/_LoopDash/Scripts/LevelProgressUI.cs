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
            fillImage.type = Image.Type.Simple;
            fillImage.rectTransform.sizeDelta = new Vector2(0f, fillImage.rectTransform.sizeDelta.y);
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
        float progress = level != null ? Mathf.Clamp(level.Progress, 0f, 100f) : 0f;
        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            RectTransform trackRect = fillRect.parent as RectTransform;
            fillImage.type = Image.Type.Simple;
            if (trackRect != null)
            {
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(trackRect.rect.width * progress / 100f, 0f);
            }
        }
        if (percentageLabel != null) percentageLabel.text = $"{Mathf.RoundToInt(progress)}%";
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
