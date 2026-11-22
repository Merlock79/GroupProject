using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the existing CubeExplosion cooldown without controlling it.</summary>
public class ExplosionCooldownUI : MonoBehaviour
{
    [SerializeField] private CubeExplosion explosion;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text statusLabel;

    private void OnEnable()
    {
        if (PlayerModeManager.Instance != null)
        {
            PlayerModeManager.Instance.OnPlayerChanged -= HandlePlayerChanged;
            PlayerModeManager.Instance.OnPlayerChanged += HandlePlayerChanged;
        }
        ResolveReferences();
        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerModeManager.Instance != null)
            PlayerModeManager.Instance.OnPlayerChanged -= HandlePlayerChanged;
    }

    private void HandlePlayerChanged(GameObject player)
    {
        explosion = player != null ? player.GetComponent<CubeExplosion>() : null;
        Refresh();
    }

    private void Update()
    {
        if (explosion == null) ResolveReferences();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (explosion == null) explosion = FindAnyObjectByType<CubeExplosion>();
        if (fillImage == null)
        {
            Transform fill = transform.Find("Track/Fill");
            if (fill != null) fillImage = fill.GetComponent<Image>();
        }
        if (statusLabel == null)
        {
            Transform label = transform.Find("Status");
            if (label != null) statusLabel = label.GetComponent<TMP_Text>();
        }
    }

    private void Refresh()
    {
        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.rectTransform;
            RectTransform trackRect = fillRect.parent as RectTransform;
            if (trackRect != null)
            {
                float progress = explosion != null ? explosion.CooldownProgress : 0f;
                fillImage.type = Image.Type.Simple;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(trackRect.rect.width * progress, 0f);
            }
        }

        if (statusLabel != null)
        {
            if (explosion == null)
                statusLabel.text = "—";
            else if (!explosion.IsUnlocked)
                statusLabel.text = "ЗАКРИТО";
            else if (explosion.IsReady)
                statusLabel.text = "ГОТОВО";
            else
                statusLabel.text = $"{Mathf.RoundToInt(explosion.CooldownProgress * 100f)}%";
        }
    }
}
