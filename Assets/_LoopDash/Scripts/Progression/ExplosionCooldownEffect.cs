using System.Globalization;
using UnityEngine;

[CreateAssetMenu(menuName = "Loop Runner/Progression/Effects/Explosion Cooldown")]
public sealed class ExplosionCooldownEffect : UpgradeEffect
{
    [SerializeField, Min(0.1f)] private float baseCooldown = 1.5f;
    [SerializeField, Min(0f)] private float reductionPerLevel = 0.1f;

    public float GetCooldown(int level)
    {
        return Mathf.Max(0.1f, baseCooldown - reductionPerLevel * level);
    }

    public override string FormatValue(int level)
    {
        return GetCooldown(level).ToString("0.##", CultureInfo.InvariantCulture) + " s";
    }
}
