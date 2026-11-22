using UnityEngine;

[CreateAssetMenu(menuName = "Loop Runner/Progression/Effects/Double Coin Chance")]
public sealed class DoubleCoinChanceEffect : UpgradeEffect
{
    public override string FormatValue(int level)
    {
        return $"{GetChance(level) * 100f:0}%";
    }

    public float GetChance(int level)
    {
        return 0.05f * level;
    }

    public bool ShouldDouble(int level)
    {
        float chance = GetChance(level);
        return chance > 0f && Random.value < chance;
    }
}
