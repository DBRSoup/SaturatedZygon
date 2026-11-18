using System.Collections.Generic;

namespace SaturatedZygon;

public class UpgradeProperty_SaturatedZygon : UpgradeProperty
{
    public Range<float> saturationDurationRange = new(8f, 10f);
    public Range<float> killReduction = new(-0.8f, -0.75f);
    public Range<float> saturationBoostRange = new(0.20f, 0.25f);

    public float turboSaturation = -2f;

    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        Range<float> saturationDuration = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(saturationDurationRange, upgrade, out var saturationDurationRatio);
        yield return StatData.Create("Saturation Duration", saturationDuration, upgrade, ref rand, OverrideType.Add).WithPatternRatio(saturationDurationRatio);

        Range<float> saturationBoost = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(saturationBoostRange, upgrade, out var saturationBoostRatio);
        yield return StatData.Create("Saturation Damage", saturationBoost + 1f, upgrade, ref rand, OverrideType.Multiply).WithPatternRatio(saturationBoostRatio);

        Range<float> killProgress = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(killReduction, upgrade, out var killReductionRatio, invert: true);
        yield return StatData.Create("Stacks on Kill", killProgress + 1, upgrade, ref rand, OverrideType.Multiply).WithPatternRatio(killReductionRatio);

        if (upgrade != null && upgrade.IsTurbocharged)
        {
            yield return UpgradeProperty.AddTurbochargedProperty(upgrade);

            yield return StatData.Create("Saturation Duration", turboSaturation, OverrideType.Add);

            yield return new StatData("Thrown discs cycle between elements while <color=#ccffcc>saturated</color>");
        }
    }

    public override string GetTurbochargedInfo(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        string text = StatData.Create("SATURATION DURATION", turboSaturation, OverrideType.Add).ToString(upgrade, showExtraInfo: true);
        text += "\n";
        text += "Thrown discs cycle between elements while <color=#ccffcc>saturated</color>";

        return text;
    }

    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (gear is PhotonDisc photonDisc)
        {
            var stats = ZygonUpgrade.GetStats(photonDisc);

            Range<float> saturationDuration = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(saturationDurationRange, upgrade, out _);
            stats.saturationDuration = saturationDuration.GetValue(ref rand, upgrade);

            Range<float> saturationBoost = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(saturationBoostRange, upgrade, out _);
            stats.saturationBoost = saturationBoost.GetValue(ref rand, upgrade);

            Range<float> killProgress = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(killReduction, upgrade, out _);
            stats.killReduction = killProgress.GetValue(ref rand, upgrade);

            if (upgrade != null && upgrade.IsTurbocharged)
            {
                stats.saturationDuration += turboSaturation;

                stats.turboSaturation = true;
            }
        }
    }

    public override void Remove(IGear gear, IGear prefab, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (gear is PhotonDisc photonDisc)
        {
            ZygonUpgrade.ResetPluginStats(photonDisc);
        }
    }
}
