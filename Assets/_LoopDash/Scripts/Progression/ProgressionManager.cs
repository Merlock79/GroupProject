using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProgressionManager
{
    private const string TreeResourcePath = "LoopRunnerProgressionTree";

    private readonly Dictionary<UpgradeDefinition, int> levels = new Dictionary<UpgradeDefinition, int>();

    public static ProgressionManager Instance { get; private set; }
    public ProgressionTree Tree { get; }
    public event Action ProgressionChanged;

    private ProgressionManager(ProgressionTree tree)
    {
        tree.Validate();
        Tree = tree;

        Dictionary<string, int> savedLevels = ProgressionSave.Load();
        foreach (UpgradeDefinition upgrade in Tree.Upgrades)
        {
            savedLevels.TryGetValue(upgrade.Id, out int level);
            levels.Add(upgrade, Mathf.Clamp(level, 0, upgrade.MaxLevel));
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Instance = null;
        ProgressionTree tree = Resources.Load<ProgressionTree>(TreeResourcePath);
        if (tree == null)
            throw new InvalidOperationException($"Progression tree resource '{TreeResourcePath}' is missing.");

        Instance = new ProgressionManager(tree);
    }

    public int GetLevel(UpgradeDefinition upgrade)
    {
        return levels[upgrade];
    }

    internal int GetActiveLevel(UpgradeDefinition upgrade)
    {
        int level = GetLevel(upgrade);
        return level > 0 && MeetsRequirements(upgrade) ? level : 0;
    }

    public bool IsUnlocked(UpgradeDefinition upgrade)
    {
        if (!levels.ContainsKey(upgrade))
            throw new ArgumentException("Upgrade does not belong to this progression tree.", nameof(upgrade));

        return MeetsRequirements(upgrade);
    }

    public bool CanPurchase(UpgradeDefinition upgrade)
    {
        int level = GetLevel(upgrade);
        return level < upgrade.MaxLevel
            && MeetsRequirements(upgrade)
            && CoinStorage.CanAfford(upgrade.GetCost(level));
    }

    public bool Purchase(UpgradeDefinition upgrade)
    {
        int level = GetLevel(upgrade);
        if (level >= upgrade.MaxLevel || !MeetsRequirements(upgrade))
            return false;

        int newLevel = level + 1;
        bool purchased = CoinStorage.TrySpendCoins(upgrade.GetCost(level), () =>
        {
            ProgressionSave.StagePurchase(levels, upgrade, newLevel);
            levels[upgrade] = newLevel;
        });

        if (purchased)
            ProgressionChanged?.Invoke();

        return purchased;
    }

    public void ResetProgression()
    {
        ProgressionSave.Clear();
        foreach (UpgradeDefinition upgrade in Tree.Upgrades)
            levels[upgrade] = 0;

        ProgressionChanged?.Invoke();
    }

    private bool MeetsRequirements(UpgradeDefinition upgrade)
    {
        foreach (UpgradeDefinition.Requirement requirement in upgrade.Requirements)
        {
            if (GetLevel(requirement.Upgrade) < requirement.Level)
                return false;
        }

        return true;
    }
}
