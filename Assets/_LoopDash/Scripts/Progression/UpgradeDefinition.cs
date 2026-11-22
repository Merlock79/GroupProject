using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loop Runner/Progression/Upgrade")]
public sealed class UpgradeDefinition : ScriptableObject
{
    [Serializable]
    public struct Requirement
    {
        [SerializeField] private UpgradeDefinition upgrade;
        [SerializeField, Min(1)] private int level;

        public UpgradeDefinition Upgrade => upgrade;
        public int Level => level;
    }

    [SerializeField, Tooltip("Unique save ID. Keep it unchanged after release.")]
    private string id;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxLevel = 1;
    [SerializeField, Min(0), Tooltip("Unrounded price of level 1. Zero makes every level free.")]
    private int basePrice = 10;
    [SerializeField, Min(1f), Tooltip("Price growth per level. A value of 2 doubles the unrounded price each level.")]
    private float priceMultiplier = 1.85f;
    [SerializeField] private Requirement[] requirements = Array.Empty<Requirement>();
    [SerializeField] private UpgradeEffect effect;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxLevel => maxLevel;
    public int BasePrice => basePrice;
    public float PriceMultiplier => priceMultiplier;
    public IReadOnlyList<Requirement> Requirements => requirements;
    public UpgradeEffect Effect => effect;

    public int GetCost(int currentLevel)
    {
        if (currentLevel < 0 || currentLevel >= maxLevel)
            throw new ArgumentOutOfRangeException(nameof(currentLevel));

        if (basePrice == 0)
            return 0;

        double price = basePrice * Math.Pow(priceMultiplier, currentLevel);
        int step = price < 100d ? 5 : 10;
        // Keep large configured prices inside the integer range before converting.
        int maximumCost = int.MaxValue / step * step;
        double cappedPrice = Math.Min(price, maximumCost);
        return (int)(Math.Ceiling(cappedPrice / step) * step);
    }
}
