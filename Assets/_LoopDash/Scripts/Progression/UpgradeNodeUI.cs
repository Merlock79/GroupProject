using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class UpgradeNodeUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image icon;
    [SerializeField] private TMPro.TextMeshProUGUI nameLabel;
    [SerializeField] private TMPro.TextMeshProUGUI levelLabel;
    [SerializeField] private TMPro.TextMeshProUGUI effectValueLabel;
    [SerializeField] private TMPro.TextMeshProUGUI detailsLabel;

    private UnityEngine.UI.Button button;
    private UpgradeDefinition upgrade;
    private ProgressionManager manager;

    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(Purchase);
    }

    internal void Bind(UpgradeDefinition definition, ProgressionManager progressionManager)
    {
        upgrade = definition;
        manager = progressionManager;
        icon.sprite = upgrade.Icon;
        icon.enabled = icon.sprite != null;
        nameLabel.text = upgrade.DisplayName;
        effectValueLabel.gameObject.SetActive(upgrade.Effect != null);
    }

    internal void Refresh()
    {
        int level = manager.GetLevel(upgrade);
        bool isMaxLevel = level == upgrade.MaxLevel;
        bool isUnlocked = manager.IsUnlocked(upgrade);
        levelLabel.text = $"{level}/{upgrade.MaxLevel}";
        button.interactable = manager.CanPurchase(upgrade);

        UpgradeEffect effect = upgrade.Effect;
        if (effect != null)
        {
            string currentValue = effect.FormatValue(isUnlocked ? level : 0);
            effectValueLabel.text = isMaxLevel ? currentValue : $"{currentValue} → {effect.FormatValue(level + 1)}";
        }

        if (isMaxLevel)
        {
            detailsLabel.text = isUnlocked ? "Max level" : "Locked";
            return;
        }

        int cost = upgrade.GetCost(level);
        string price = cost == 0 ? "Free" : $"Cost {cost}";
        string status = "Available";
        if (!isUnlocked)
            status = "Locked";
        else if (!button.interactable)
            status = "Not enough coins";

        detailsLabel.text = $"{price}  |  {status}";
    }

    private void Purchase()
    {
        manager.Purchase(upgrade);
    }
}
