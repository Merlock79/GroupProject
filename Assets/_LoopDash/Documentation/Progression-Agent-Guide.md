# Implementing Loop Runner upgrades

An upgrade consists of an `UpgradeDefinition` asset registered in the active `ProgressionTree`, an optional `UpgradeEffect` asset, and a gameplay consumer for each behavior the upgrade changes. The existing manager owns purchase, balance, and save state. The view builds nodes from the active tree.

## Source map

| Responsibility | Source |
| --- | --- |
| Serialized node fields, prices, and prerequisites | [UpgradeDefinition.cs](../Scripts/Progression/UpgradeDefinition.cs) |
| Active catalog | [LoopRunnerProgressionTree.asset](../Resources/LoopRunnerProgressionTree.asset) |
| Validation, purchased levels, and purchase API | [ProgressionTree.cs](../Scripts/Progression/ProgressionTree.cs) · [ProgressionManager.cs](../Scripts/Progression/ProgressionManager.cs) |
| Save format and wallet | [ProgressionSave.cs](../Scripts/Progression/ProgressionSave.cs) · [CoinStorage.cs](../Scripts/CoinStorage.cs) |
| Effect value preview, placement, and links | [UpgradeNodeUI.cs](../Scripts/Progression/UpgradeNodeUI.cs) · [ProgressionTreeUI.cs](../Scripts/Progression/ProgressionTreeUI.cs) · [ProgressionGraphLayout.cs](../Scripts/Progression/ProgressionGraphLayout.cs) |

## Register the node

1. Inspect the existing mechanic and its entry points. Choose the starting value, per-level behavior, maximum level, prices, prerequisites, and which actions should receive the effect.
2. Create an `UpgradeDefinition` asset under `Assets/_LoopDash/Progression`. Set a unique, nonblank, stable `Id` with no leading or trailing whitespace. IDs are case-sensitive and serve as save keys. Use a name such as `explosion.radius` and keep published IDs stable.
3. Set the display name, description, optional `Sprite` icon, and a positive `MaxLevel`. Configure a nonnegative integer `BasePrice` and a finite `PriceMultiplier` of at least 1. New definitions default to base 10 and multiplier 1.85. A zero base makes every level free. For a one-time unlock use `MaxLevel = 1`.
4. For each prerequisite, reference the other `UpgradeDefinition` asset and a level from 1 through its `MaxLevel`. The node requires all entries. Keep the graph acyclic and register every referenced definition in the active tree. Leave the prerequisites empty for a root.
5. Add the new definition to `Upgrades` on the active tree. The manager loads and validates this resource before the first scene. The next Play Mode launch reloads the list and builds the graph from it. [Active tree](../Resources/LoopRunnerProgressionTree.asset)

To create siblings, use the same prerequisite on each sibling. To connect branches, list all required parents. The longest prerequisite chain determines the vertical tier. For layout ownership the deepest prerequisite wins, with the first equally deep requirement taking precedence. `Upgrades` order determines the order of roots and siblings within that layout. The view draws a link for every requirement and computes positions from the assets. [Layout implementation](../Scripts/Progression/ProgressionGraphLayout.cs)

### Serialized assets

Use Unity's **Create → Loop Runner → Progression → Upgrade** when authoring in the Inspector. For text-based edits, follow the asset shape in the repository and retain the existing GUIDs of edited assets. Give each new script and asset its own `.meta` and unique GUID. The `m_Script` reference uses the GUID from the corresponding script's `.cs.meta` with `fileID: 11500000` and `type: 3`. Definition, prerequisite, and effect references use the respective asset GUID with `fileID: 11400000` and `type: 2`. Definition assets store scalar fields `basePrice` and `priceMultiplier` for pricing. Resolve each GUID to its `.meta` before reporting the asset as wired. [Definition example](../Progression/DoubleCoinChanceUpgrade.asset) · [Effect example](../Progression/DoubleCoinChanceEffect.asset)

### Price calculation

`UpgradeDefinition.GetCost(currentLevel)` calculates the next purchase price using `basePrice * Math.Pow(priceMultiplier, currentLevel)`. Level 0 refers to the first purchase. For a raw result below 100, round up to a multiple of 5. From 100 onward, round up to a multiple of 10. Examples include `43 -> 45`, `99 -> 100`, `100 -> 100`, and `115 -> 120`. Each level is calculated from the original base. A base of 10 and multiplier of 1.85 yields `10, 20, 35, 65, 120, 220, 410, 750, 1380, 2540` over ten purchases.

The method accepts levels from 0 through `MaxLevel - 1` and throws for a level outside that range. Positive bases below 5 round to 5. Extremely large results saturate at 2147483640, the largest multiple of 10 representable as an `int`. Tree validation checks the base and multiplier before normal runtime use. The three current definition assets explicitly use `basePrice: 0` for free testing. The manager's affordability check, purchase transaction and node label all call `GetCost`. Keep future callers on that method.

## Connect the effect to gameplay

- Reuse an existing effect type when its behavior fits. For a new mechanic, derive from `UpgradeEffect` under `Assets/_LoopDash/Scripts/Progression`, create an asset of that type under `Progression`, and assign it to the definition's `Effect` field. A data-only placeholder can leave `Effect` empty. [Effect contract](../Scripts/Progression/UpgradeEffect.cs)
- Put tuning and deterministic per-level calculations in the effect. Make level 0 neutral for the mechanic. Implement `FormatValue(int level)` using the same formula as gameplay. The node uses it to show the current and next values, with a single value at maximum level. Store purchased levels in the manager and runtime action state in the owning component. [Numeric effect](../Scripts/Progression/DoubleCoinChanceEffect.cs) · [Ability gate effect](../Scripts/Progression/ExplosionUnlockEffect.cs)
- Locate the component that performs the action. Give it a serialized reference to the relevant `UpgradeDefinition` and assign the asset on the prefab or scene that is used in play. Read the active level at the point of use, then apply the effect there. For an ability gate, enforce it at the action's entry point and reflect that gate in its readiness UI. Trace all callers that should share the behavior. [Coin reward consumer](../Scripts/Coin.cs) · [Explosion consumer](../Scripts/CubeExplosion.cs) · [Explosion HUD](../Scripts/ExplosionCooldownUI.cs)

A future explosion-radius upgrade could use the following effect type. Configure its definition to require level 1 of `ExplosionUnlockUpgrade`. Use the same effective radius for the blast overlap and related runtime indicators, and use the base radius for the edit-time gizmo. The numerical values here are illustrative.

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Loop Runner/Progression/Effects/Explosion Radius")]
public sealed class ExplosionRadiusEffect : UpgradeEffect
{
    [SerializeField, Min(0f)] private float bonusPerLevel = 0.5f;

    public float GetBonus(int level)
    {
        return bonusPerLevel * level;
    }

    public override string FormatValue(int level)
    {
        return $"+{GetBonus(level):0.##}";
    }
}
```

At the runtime call site, read `manager.GetActiveLevel(radiusUpgrade)` and add `effect.GetBonus(level)` to the base radius. `radiusUpgrade` is the assigned definition and `effect` is its `ExplosionRadiusEffect`.

The existing cooldown effect owns both `baseCooldown` and `reductionPerLevel`. Its `GetCooldown(level)` returns the effective duration with a minimum of 0.1 seconds. `CubeExplosion.Cooldown` reads it for the active level, including level 0 before the upgrade is unlocked. Remaining time uses the last explosion timestamp and the current configured duration, so level changes affect an ongoing cooldown immediately. The node's value preview uses the same clamped formula. The `ExplosionCooldownUpgrade` asset retains the save ID `explosion.placeholder.1` and its original GUID to preserve a previously purchased placeholder level. [Cooldown effect](../Scripts/Progression/ExplosionCooldownEffect.cs) · [Cooldown definition](../Progression/ExplosionCooldownUpgrade.asset)

## Purchase and save semantics

- `GetLevel(upgrade)` returns the purchased level. `IsUnlocked(upgrade)` reports whether the direct requirements are met, including for a level-0 root. `GetActiveLevel(upgrade)` returns the purchased level when the direct requirements are met, and otherwise returns 0. The requirement check compares stored levels of the referenced definitions. Use an active-level check for gameplay that should pause behind a newly added prerequisite.
- Use `CanPurchase(upgrade)` for availability and `Purchase(upgrade)` to perform the transaction. The manager checks prerequisites and the level cap, and the wallet checks affordability. A successful purchase spends coins, saves the level, and raises `ProgressionChanged`. The wallet has fractional balances and node prices are currently whole coins.
- Save data uses `Id` and level in the `LoopRunner.Progression` PlayerPrefs entry. Loading clamps levels to the current valid range and picks up IDs present in the active tree. When adding a requirement to an existing node, account for players who already own levels of that node. `ResetProgression()` clears the purchased levels and preserves the wallet. [Manager](../Scripts/Progression/ProgressionManager.cs) · [Save code](../Scripts/Progression/ProgressionSave.cs)

## Verification

- Check unique IDs and GUIDs, all registered references, valid base prices and multipliers, valid required levels, the concrete effect type, and the serialized reference on the actual gameplay prefab or scene.
- In Unity Play Mode open **MainMenu → Progression**. Verify the node, its links, disabled states for unmet requirements and insufficient coins, the level and balance after purchase, the gameplay effect at level 0 and a purchased level, and the maximum-level state.
- Re-enter Play Mode to verify saved levels and gameplay behavior. Inspect `GetLevel`, `IsUnlocked` and `CanPurchase` through the debugger when diagnosing node state. For a development reset call `ProgressionManager.Instance.ResetProgression()`, which clears the current save's upgrade levels and preserves the wallet.
- Report which checks were performed and any remaining Unity Play Mode verification.
