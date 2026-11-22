using System.Collections.Generic;
using UnityEngine;

internal sealed class ProgressionGraphLayout
{
    private readonly Dictionary<UpgradeDefinition, Vector2> positions = new Dictionary<UpgradeDefinition, Vector2>();
    private readonly Vector2 nodeSize;
    private readonly float padding;
    private readonly float siblingSpacing;
    private readonly float tierSpacing;

    internal Vector2 Size { get; }
    internal int TierCount { get; }
    internal Vector2 this[UpgradeDefinition upgrade] => positions[upgrade];

    internal ProgressionGraphLayout(
        IReadOnlyList<UpgradeDefinition> upgrades, Vector2 nodeSize, float padding, float siblingSpacing, float tierSpacing)
    {
        this.nodeSize = nodeSize;
        this.padding = padding;
        this.siblingSpacing = siblingSpacing;
        this.tierSpacing = tierSpacing;

        var branches = new Dictionary<UpgradeDefinition, Branch>();
        var roots = new List<Branch>();
        foreach (UpgradeDefinition upgrade in upgrades)
        {
            Branch branch = GetBranch(upgrade, branches);
            if (branch.Parent == null)
                roots.Add(branch);
            else
                branch.Parent.Children.Add(branch);

            TierCount = Mathf.Max(TierCount, branch.Tier + 1);
        }

        float nextRoot = padding;
        foreach (Branch root in roots)
        {
            Measure(root);
            Place(root, nextRoot);
            nextRoot += root.Width + siblingSpacing;
        }

        float width = nextRoot + padding - (roots.Count > 0 ? siblingSpacing : 0f);
        float height = 2f * padding + TierCount * nodeSize.y + Mathf.Max(0, TierCount - 1) * tierSpacing;
        Size = new Vector2(width, height);
    }

    internal float GetTierY(int tier)
    {
        return -padding - nodeSize.y * 0.5f - tier * (nodeSize.y + tierSpacing);
    }

    internal void GetLinkEndpoints(UpgradeDefinition parent, UpgradeDefinition child,
        out Vector2 start, out Vector2 end)
    {
        float halfHeight = nodeSize.y * 0.5f;
        start = positions[parent] + Vector2.down * halfHeight;
        end = positions[child] + Vector2.up * halfHeight;
    }

    private static Branch GetBranch(UpgradeDefinition upgrade, Dictionary<UpgradeDefinition, Branch> branches)
    {
        if (branches.TryGetValue(upgrade, out Branch branch))
            return branch;

        Branch parent = null;
        foreach (UpgradeDefinition.Requirement requirement in upgrade.Requirements)
        {
            Branch candidate = GetBranch(requirement.Upgrade, branches);
            if (parent == null || candidate.Tier > parent.Tier)
                parent = candidate;
        }

        // The deepest prerequisite owns the branch footprint. All prerequisites still get a visible link.
        branch = new Branch(upgrade, parent, parent == null ? 0 : parent.Tier + 1);
        branches.Add(upgrade, branch);
        return branch;
    }

    private float Measure(Branch branch)
    {
        float childrenWidth = 0f;
        foreach (Branch child in branch.Children)
            childrenWidth += Measure(child) + siblingSpacing;

        branch.Width = Mathf.Max(nodeSize.x, childrenWidth - siblingSpacing);
        return branch.Width;
    }

    private void Place(Branch branch, float left)
    {
        positions.Add(branch.Upgrade, new Vector2(left + branch.Width * 0.5f, GetTierY(branch.Tier)));
        foreach (Branch child in branch.Children)
        {
            Place(child, left);
            left += child.Width + siblingSpacing;
        }
    }

    private sealed class Branch
    {
        internal readonly UpgradeDefinition Upgrade;
        internal readonly Branch Parent;
        internal readonly int Tier;
        internal readonly List<Branch> Children = new List<Branch>();
        internal float Width;

        internal Branch(UpgradeDefinition upgrade, Branch parent, int tier)
        {
            Upgrade = upgrade;
            Parent = parent;
            Tier = tier;
        }
    }
}
