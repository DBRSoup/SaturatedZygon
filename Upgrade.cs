using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SaturatedZygon
{
    public enum SaturationType
    {
        None,
        Full,
        Frenzied,
        Waning
    }
    public static class ZygonUpgrade
    {
        private static readonly ConditionalWeakTable<PhotonDisc, PluginUpgradeStats> upgradeStats = [];

        public static PluginUpgradeStats GetStats(PhotonDisc disc) => upgradeStats.GetOrCreateValue(disc);

        public static void ResetPluginStats(PhotonDisc disc)
        {
            upgradeStats.Remove(disc);
        }
    }

    public class PluginUpgradeStats
    {
        public struct EffectProgress
        {
            public float Time;
            public float Progress;
        }

        public SaturationType saturationType = SaturationType.None;

        public SaturationType prevType = SaturationType.None;

        public float maxStackTimer = 10f;
        public float buffDuration = -1f;

        public bool turboSaturation = false;
        public bool turboFrenzy = false;
        public float killReduction;
        public float saturationDuration;
        public float frenzyDuration;
        public float waningDuration;

        public float saturationBoost;
        public float frenzyBoost;
        public float frenzySpeed;

        public float buffTimer = -1f;

        public const float vignetteDuration = 0.3f;

        public readonly Dictionary<IDamageSource, Dictionary<EffectType, EffectProgress>> GunZygonProgress = [];
        public EffectType zygonDiscEffect = EffectType.Fire;

        public override string ToString()
        {
            string progress = string.Join(", ",
                GunZygonProgress.Select(source =>
                    $"[{source.Key}: {string.Join(", ", source.Value.Select(effect =>
                        $"{effect.Key} (Time={effect.Value.Time:F2}, Progress={effect.Value.Progress:P1})"))}]"
                ));

            return
                $"PluginUpgradeStats {{\n" +
                $"  SaturationType: {saturationType},\n" +
                $"  FullSaturationTimer: {maxStackTimer:F2},\n" +
                $"  BuffDuration: {buffDuration:F2},\n" +
                $"  SaturationDuration: {saturationDuration:F2},\n" +
                $"  FrenzyDuration: {frenzyDuration:F2},\n" +
                $"  WaningDuration: {waningDuration:F2},\n" +
                $"  SaturationBoost: {saturationBoost:F2},\n" +
                $"  FrenzyBoost: {frenzyBoost:F2},\n" +
                $"  BuffTimer: {buffTimer:F2},\n" +
                $"  ZygonDiscEffect: {zygonDiscEffect},\n" +
                $"  GunZygonProgress: {{ {progress} }}\n" +
                $"}}";
        }

    }
}
