public sealed class ExplosionUnlockEffect : UpgradeEffect
{
    public bool IsUnlocked(int level)
    {
        return level > 0;
    }

    public override string FormatValue(int level)
    {
        return IsUnlocked(level) ? "Unlocked" : "Locked";
    }
}
