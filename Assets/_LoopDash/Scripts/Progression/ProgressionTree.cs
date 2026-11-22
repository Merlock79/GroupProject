using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loop Runner/Progression/Tree")]
public sealed class ProgressionTree : ScriptableObject
{
    [SerializeField] private UpgradeDefinition[] upgrades = Array.Empty<UpgradeDefinition>();

    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

    internal void Validate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var nodes = new HashSet<UpgradeDefinition>();

        foreach (UpgradeDefinition upgrade in upgrades)
        {
            if (upgrade == null)
                throw new InvalidOperationException($"Progression tree '{name}' contains a missing upgrade.");

            string id = upgrade.Id;
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new InvalidOperationException($"Upgrade '{upgrade.name}' needs a non-empty ID without leading or trailing whitespace.");

            if (!ids.Add(id))
                throw new InvalidOperationException($"Progression tree '{name}' contains duplicate upgrade ID '{id}'.");

            if (upgrade.MaxLevel < 1)
                throw new InvalidOperationException($"Upgrade '{id}' needs a positive maximum level.");

            if (upgrade.BasePrice < 0 || !float.IsFinite(upgrade.PriceMultiplier) || upgrade.PriceMultiplier < 1f)
                throw new InvalidOperationException($"Upgrade '{id}' needs a nonnegative base price and a finite price multiplier of at least 1.");

            nodes.Add(upgrade);
        }

        var visiting = new HashSet<UpgradeDefinition>();
        var visited = new HashSet<UpgradeDefinition>();
        foreach (UpgradeDefinition upgrade in upgrades)
            ValidateDependencies(upgrade, nodes, visiting, visited);
    }

    private static void ValidateDependencies(
        UpgradeDefinition upgrade,
        HashSet<UpgradeDefinition> nodes,
        HashSet<UpgradeDefinition> visiting,
        HashSet<UpgradeDefinition> visited)
    {
        if (visited.Contains(upgrade))
            return;

        if (!visiting.Add(upgrade))
            throw new InvalidOperationException($"Progression dependencies contain a cycle through upgrade '{upgrade.Id}'.");

        foreach (UpgradeDefinition.Requirement requirement in upgrade.Requirements)
        {
            UpgradeDefinition dependency = requirement.Upgrade;
            if (dependency == null || !nodes.Contains(dependency))
                throw new InvalidOperationException($"Upgrade '{upgrade.Id}' requires an upgrade outside its progression tree.");

            if (requirement.Level < 1 || requirement.Level > dependency.MaxLevel)
                throw new InvalidOperationException($"Upgrade '{upgrade.Id}' requires invalid level {requirement.Level} of '{dependency.Id}'.");

            ValidateDependencies(dependency, nodes, visiting, visited);
        }

        visiting.Remove(upgrade);
        visited.Add(upgrade);
    }
}
