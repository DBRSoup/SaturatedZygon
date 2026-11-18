using System.Collections.Generic;

namespace SaturatedZygon;

public class UpgradeProperty_ZygonFrenzy : UpgradeProperty
{

    public Range<float> frenzyDurationRange = new(10f, 12f);
    public Range<float> frenzyBoostRange = new(0.20f, 0.25f);

    public float waningDuration = 5f;

    public float speedBonus = 5f;

    public override IEnumerator<StatData> GetStatData(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        Range<float> frenzyDuration = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(frenzyDurationRange, upgrade, out var frenzyDurationRatio);
        yield return StatData.Create("Frenzy Duration", frenzyDuration, upgrade, ref rand, OverrideType.Add).WithPatternRatio(frenzyDurationRatio);

        Range<float> frenzyBoost = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(frenzyBoostRange, upgrade, out var frenzyBoostRatio);
        yield return StatData.Create("Frenzy Damage", frenzyBoost + 1f, upgrade, ref rand, OverrideType.Multiply).WithPatternRatio(frenzyBoostRatio);

        yield return StatData.Create("Waning Duration", waningDuration, OverrideType.Add);


        if (upgrade != null && upgrade.IsTurbocharged)
        {
            yield return UpgradeProperty.AddTurbochargedProperty(upgrade);

            yield return StatData.Create("Speed while frenzied", speedBonus, OverrideType.Add);

            yield return new StatData("Discs may <color=#D78FFF>decay</color> while <color=#DC3132>frenzied</color>");
        }
    }

    public override string GetTurbochargedInfo(Pigeon.Math.Random rand, IUpgradable gear, UpgradeInstance upgrade)
    {
        string text = StatData.Create("SPEED WHILE FRENZIED", speedBonus, OverrideType.Add).ToString(upgrade, showExtraInfo: true);

        text += "\n\n";
        text += "Discs may <color=#D78FFF>decay</color> while <color=#DC3132>frenzied</color>";
        return text;
    }

    public override void Apply(IGear gear, UpgradeInstance upgrade, ref Pigeon.Math.Random rand)
    {
        if (gear is PhotonDisc photonDisc)
        {
            var stats = ZygonUpgrade.GetStats(photonDisc);
            stats.waningDuration = waningDuration;

            Range<float> frenzyDuration = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(frenzyDurationRange, upgrade, out _);
            stats.frenzyDuration = frenzyDuration.GetValue(ref rand, upgrade);

            Range<float> frenzyBoost = UpgradeProperty.ModifyAdditiveRangeFromPatternSize(frenzyBoostRange, upgrade, out _);
            stats.frenzyBoost = frenzyBoost.GetValue(ref rand, upgrade);

            if (upgrade != null && upgrade.IsTurbocharged)
            {
                stats.frenzySpeed = speedBonus;
                stats.turboFrenzy = true;
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
