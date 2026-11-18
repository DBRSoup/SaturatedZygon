using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SaturatedZygon.PluginUpgradeStats;

namespace SaturatedZygon;

[MycoMod(null, ModFlags.IsClientSide)]
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class ZygonPlugin : BaseUnityPlugin
{
    //public static ZygonPlugin Instance { get; private set; }
    public const string PLUGIN_GUID = "DBR.SaturatedZygon";
    public const string PLUGIN_NAME = "SaturatedZygon";
    public const string PLUGIN_VERSION = "0.4.4";

    internal static Sprite SaturatedIcon;
    internal static Sprite FrenzyIcon;
    internal static uint Ice_Shard_Sound;

    internal static readonly string gearApiName = "photondisc";
    internal static readonly int upgradeID = 19998500;

    internal static new ManualLogSource Logger;
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded");

        Harmony harmony = new(PLUGIN_GUID);
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.LogError((object)("Error applying patches: " + ex.Message));
        }

        SaturatedIcon = UpgradeHelper.LoadExternalIcon("saturatedzygon.png", this);
        FrenzyIcon = UpgradeHelper.LoadExternalIcon("zygonfrenzy.png", this);
        StartCoroutine(UpgradeHelper.LoadSoundAfterWwise("Play_Ice_Shard", id => Ice_Shard_Sound = id));

        PlayerData.AddRegisterUpgradesCallback(RegisterSaturatedZygon.RegisterUpgrade);
        PlayerData.AddRegisterUpgradesCallback(RegisterZygonFrenzy.RegisterUpgrade);
    }

    [HarmonyPatch(typeof(Keywords), nameof(Keywords.LoadKeywords))]
    public static class KeywordsPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {

            UpgradeHelper.RegisterKeyword("wane", "<color=#9370db>Waning</color> activates after <color=#ccffcc>saturation</color> or <color=#DC3132>frenzy</color> expires.\n\n" +
                                            "During <color=#9370db>waning</color>, only <color=#ccffcc>saturation</color> buffs remain active and elements can stack Saturated Zygon and Zygon Frenzy again.");
        }
    }

    #region OnUpgrades
    public static MutableDamageCallback damageCallback;
    public static KillCallback killCallback;
    public static RefAction<float> speedDelegate;


    [HarmonyPatch(typeof(PhotonDisc), nameof(PhotonDisc.OnUpgradesEnabled))]
    public static class OnUpgradesEnabledPatch
    {

        [HarmonyPostfix]
        private static void Postfix(PhotonDisc __instance)
        {
            var InstanceStats = ZygonUpgrade.GetStats(__instance);
            if (InstanceStats.saturationDuration > 0 || InstanceStats.frenzyDuration > 0)
            {


                Player player = __instance.Player;
                damageCallback = (ref DamageCallbackData data) =>
                {
                    ModifyOutgoingDamageZygon(ref data, InstanceStats, __instance);
                };

                killCallback = (in KillCallbackData data) =>
                {
                    TrackKillSaturationProgress(data, InstanceStats);
                };

                player.OnBeforeDamage = (MutableDamageCallback)Delegate.Combine(player.OnBeforeDamage, damageCallback);

                if (InstanceStats.saturationDuration > 0)
                    player.OnKillTarget = (KillCallback)Delegate.Combine(player.OnKillTarget, killCallback);
            }

            if (InstanceStats.frenzySpeed > 0)
            {
                speedDelegate = new RefAction<float>((ref float speed) => ModifyMoveSpeed(ref speed, InstanceStats));
                __instance.Player.OnSetMovementSpeed += speedDelegate;
            }
        }
    }

    [HarmonyPatch(typeof(PhotonDisc), nameof(PhotonDisc.OnUpgradesDisabled))]
    public static class OnUpgradesDisabledPatch
    {

        [HarmonyPostfix]
        private static void Postfix(PhotonDisc __instance)
        {
            var InstanceStats = ZygonUpgrade.GetStats(__instance);
            if (InstanceStats.saturationDuration > 0 || InstanceStats.frenzyDuration > 0)
            {
                Player player = __instance.Player;
                player.OnBeforeDamage = (MutableDamageCallback)Delegate.Remove(player.OnBeforeDamage, damageCallback);
                player.OnKillTarget = (KillCallback)Delegate.Remove(player.OnKillTarget, killCallback);
            }

            if (InstanceStats.frenzySpeed > 0)
            {
                __instance.Player.OnSetMovementSpeed -= speedDelegate;

            }

        }
    }

    private static void ModifyMoveSpeed(ref float speed, PluginUpgradeStats stats)
    {
        if (stats.frenzySpeed > 0 && stats.saturationType == SaturationType.Frenzied)
        {
            speed += stats.frenzySpeed;
        }
    }

    #endregion

    #region Update
    [HarmonyPatch(typeof(PhotonDisc), "Update")]
    public static class UpdateProgress
    {
        [HarmonyPostfix]
        public static void Postfix(PhotonDisc __instance)
        {
            var InstanceStats = ZygonUpgrade.GetStats(__instance);

            if ( (InstanceStats.saturationDuration > 0 || InstanceStats.frenzyDuration > 0)  && (InstanceStats.buffTimer == -1 || InstanceStats.waningDuration <= 0 || InstanceStats.saturationType == SaturationType.Waning) )
            {
                foreach (KeyValuePair<IDamageSource, Dictionary<EffectType, EffectProgress>> entry in InstanceStats.GunZygonProgress.ToList())
                {
                    IDamageSource damageSource = entry.Key;
                    Dictionary<EffectType, EffectProgress> effects = ProcessGunEffects(InstanceStats, entry);

                    if (effects.Count == 0)
                    {
                        InstanceStats.GunZygonProgress.Remove(damageSource);
                    }

                    else if (effects.Count == 3 && effects.Values.All(effect => effect.Progress >= 1f))
                    {
                        InstanceStats.buffDuration = -1;
                        string effectName = "";
                        Color effectColor = Color.white;

                        if (InstanceStats.saturationDuration > 0 && InstanceStats.frenzyDuration > 0)
                        {
                            if (damageSource is PhotonDisc && InstanceStats.saturationType == SaturationType.Waning)
                            {
                                InstanceStats.buffDuration = InstanceStats.frenzyDuration;
                                InstanceStats.prevType = InstanceStats.saturationType;
                                InstanceStats.saturationType = SaturationType.Frenzied;

                                effectName = "Zygon - Frenzy";
                                effectColor = Color.softRed;

                            }
                            else if (damageSource is not PhotonDisc && (InstanceStats.saturationType == SaturationType.Waning || InstanceStats.buffTimer == -1))
                            {
                                InstanceStats.buffDuration = InstanceStats.saturationDuration;
                                InstanceStats.prevType = InstanceStats.saturationType;
                                InstanceStats.saturationType = SaturationType.Full;

                                effectName = "Zygon - Saturation";
                                effectColor = Color.green;


                            }
                        }

                        else if (damageSource is not PhotonDisc && InstanceStats.saturationDuration > 0)
                        {
                            InstanceStats.buffDuration = InstanceStats.saturationDuration;
                            InstanceStats.prevType = InstanceStats.saturationType;
                            InstanceStats.saturationType = SaturationType.Full;

                            effectName = "Zygon - Saturation";
                            effectColor = Color.green;
                        }

                        else if (InstanceStats.frenzyDuration > 0)
                        {
                            if (damageSource is PhotonDisc && (InstanceStats.saturationType == SaturationType.Waning || InstanceStats.buffTimer == -1))
                            {
                                InstanceStats.buffDuration = InstanceStats.frenzyDuration;
                                InstanceStats.prevType = InstanceStats.saturationType;
                                InstanceStats.saturationType = SaturationType.Frenzied;

                                effectName = "Zygon - Frenzy";
                                effectColor = Color.softRed;
                            }
                        }

                        if (InstanceStats.buffDuration > 0) 
                        {

                            __instance.Player.UpdateStackDisplay(typeof(UpgradeProperty_SaturatedZygon), effectName, SaturatedIcon, effectColor, 1, updateName: true, InstanceStats.buffDuration);
                            __instance.Player.UpdateStackDisplay(typeof(UpgradeProperty_SaturatedZygon), effectName, SaturatedIcon, effectColor, 1);

                            InstanceStats.buffTimer = 0f;
                        }

                        InstanceStats.GunZygonProgress.Clear();
                        break;
                    }
                }
            }

            if (InstanceStats.buffTimer >= 0f)
            {
                if (InstanceStats.saturationType == SaturationType.Waning && InstanceStats.buffTimer <= PluginUpgradeStats.vignetteDuration)
                {
                    Player.LocalPlayer.ShowScreenEffect(EffectType.Decay);
                }

                else if (InstanceStats.saturationType == SaturationType.Frenzied && InstanceStats.buffTimer <= PluginUpgradeStats.vignetteDuration * 2)
                {
                    Player.LocalPlayer.ShowScreenEffect(EffectType.Fire);
                }

                else if (InstanceStats.saturationType == SaturationType.Full && InstanceStats.prevType != SaturationType.Full && InstanceStats.buffTimer <= PluginUpgradeStats.vignetteDuration * 2)
                {
                    Player.LocalPlayer.ShowScreenEffect(EffectType.Immunity);
                }

                InstanceStats.buffTimer += Time.deltaTime;

                if (IsExpired(InstanceStats))
                {
                    if (InstanceStats.waningDuration > 0 && InstanceStats.saturationType != SaturationType.Waning)
                    {
                        GameManager.PlaySound(Ice_Shard_Sound, __instance.Player.InterpolatedPosition);

                        InstanceStats.buffDuration = InstanceStats.waningDuration;
                        InstanceStats.prevType = InstanceStats.saturationType;
                        InstanceStats.saturationType = SaturationType.Waning;

                        InstanceStats.buffTimer = 0;

                        Player.LocalPlayer.UpdateStackDisplay(typeof(UpgradeProperty_SaturatedZygon), "Zygon - Wane", SaturatedIcon, Color.mediumPurple, 1, updateName: true, InstanceStats.buffDuration);
                        Player.LocalPlayer.UpdateStackDisplay(typeof(UpgradeProperty_SaturatedZygon), "Zygon - Wane", SaturatedIcon, Color.mediumPurple, 1);

                    }
                    else
                    {
                        InstanceStats.buffTimer = -1f;
                        InstanceStats.buffDuration = -1f;

                        InstanceStats.zygonDiscEffect = EffectType.Fire;

                        InstanceStats.saturationType = SaturationType.None;
                        InstanceStats.prevType = SaturationType.None;
                    }
                }
            }
        }
    }

    public static Dictionary<EffectType, EffectProgress> ProcessGunEffects(PluginUpgradeStats stats, KeyValuePair<IDamageSource, Dictionary<EffectType, EffectProgress>> gun)
    {
        Dictionary<EffectType, EffectProgress> effects = gun.Value;

        foreach (KeyValuePair<EffectType, EffectProgress> effect in effects.ToList())
        {
            if (Time.time - effect.Value.Time >= stats.maxStackTimer)
            {
                effects.Remove(effect.Key);
            }
        }

        return effects;
    }

    #endregion

    #region Saturation
    [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.Add))]
    public static class TrackEffectApplication
    {
        [HarmonyPrefix]
        public static void Prefix(float amount, IDamageSource damageSource, StatusEffect __instance)
        {
            if (Player.LocalPlayer.ThrowableGear is PhotonDisc disc && __instance.Saturation < 1f )
            {
                var InstanceStats = ZygonUpgrade.GetStats(disc);
                if (InstanceStats.saturationBoost > 0 || InstanceStats.frenzyBoost > 0)
                {
                    float leftoverAmount = 10 * (1 - __instance.Saturation);
                    float stackAmount = Mathf.Min(amount, leftoverAmount);

                    TrackSaturationProgress(InstanceStats, damageSource, __instance.Type, stackAmount);
                }
            }

        }
    }
   

    //But but but you can condense all of these conditions in one if statement!!!
    //THIS IS MORE READABLE!!
    public static void TrackSaturationProgress(PluginUpgradeStats InstanceStats, IDamageSource damageSource, EffectType effect, float effectAmount)
    {
        if (InstanceStats.saturationDuration > 0 && InstanceStats.frenzyDuration > 0)
        {
            if( (InstanceStats.buffTimer == -1 && damageSource is not PhotonDisc) || InstanceStats.saturationType == SaturationType.Waning)
            {
                AddSaturationProgress(effectAmount, effect, InstanceStats, damageSource);
            }
        }
        else if (InstanceStats.saturationDuration > 0 && damageSource is not PhotonDisc)
        {
            AddSaturationProgress(effectAmount, effect, InstanceStats, damageSource);
        }

        else if (InstanceStats.frenzyDuration > 0 && damageSource is PhotonDisc)
        {
            if ( InstanceStats.buffTimer == -1 || InstanceStats.saturationType == SaturationType.Waning )
            {
                AddSaturationProgress(effectAmount, effect, InstanceStats, damageSource);
            }

        }
    }

    public static void TrackKillSaturationProgress(KillCallbackData data, PluginUpgradeStats InstanceStats)
    {
        float amount = data.damageData.effectAmount;

        if (InstanceStats.saturationDuration > 0)
        {
            amount *= 1f + InstanceStats.killReduction;
        }

        TrackSaturationProgress(InstanceStats, data.source, data.damageData.effect, amount);
    }

    public static void AddSaturationProgress(float effectAmount, EffectType effect, PluginUpgradeStats InstanceStats, IDamageSource damageSource)
    {
        float actualAmount = effectAmount * 0.1f;

        if (effect > EffectType.Normal && effect <= EffectType.Acid)
        {
            if (!InstanceStats.GunZygonProgress.TryGetValue(damageSource, out Dictionary<EffectType, EffectProgress> effectProgressData))
            {
                effectProgressData = [];
                InstanceStats.GunZygonProgress[damageSource] = effectProgressData;
            }

            if (!effectProgressData.TryGetValue(effect, out EffectProgress progress))
            {

                progress.Time = Time.time;
                progress.Progress = Math.Clamp(actualAmount, 0f, 1f);

            }
            else if (progress.Progress >= 2f)
            {
                progress.Time = Time.time;
                progress.Progress = 1f;
            }
            else
            {
                progress.Progress += actualAmount;
            }

            effectProgressData[effect] = progress;
        }
    }

    #endregion

    #region OutgoingModifiers
    private static void ModifyOutgoingDamageZygon(ref DamageCallbackData data, PluginUpgradeStats stats, PhotonDisc disc)
    {
        if (stats.buffTimer >= 0)
        {
            //Don't modify attuned damage
            if (data.damageData.effect == disc.GrenadeData.attunedElement)
            {
                return;
            }
            else
            {
                data.damageData.damage *= GetDamageModifier(stats);
            }
        }
    }


    [HarmonyPatch(typeof(PhotonDisc), nameof(PhotonDisc.ModifyBulletData))]
    public static class ModifyOutgoingDisc
    {
        [HarmonyPostfix]
        private static void Postfix(ref BulletData data, PhotonDisc __instance)
        {
            var InstanceStats = ZygonUpgrade.GetStats(__instance);
            if (InstanceStats.buffTimer >= 0)
            {
                if (InstanceStats.turboSaturation)
                {
                    int effectCount = (InstanceStats.turboFrenzy && InstanceStats.saturationType == SaturationType.Frenzied ) ? 4 : 3;
                    InstanceStats.zygonDiscEffect = (EffectType)(( (int)InstanceStats.zygonDiscEffect % effectCount) + 1);
                    data.damageEffect = InstanceStats.zygonDiscEffect;
                }
                else if (InstanceStats.turboFrenzy && InstanceStats.saturationType == SaturationType.Frenzied && UnityEngine.Random.Range(0, 4) == 0)
                {
                    data.damageEffect = EffectType.Decay;
                }
            }
        }
    }

    public static float GetDamageModifier(PluginUpgradeStats stats)
    {
        float damageBoost = 1f;

        if (stats.saturationDuration > 0)
            damageBoost += stats.saturationBoost;

        if (stats.frenzyDuration > 0 && stats.saturationType == SaturationType.Frenzied)
            damageBoost += stats.frenzyBoost;

        return damageBoost;
    }

    #endregion

    #region Timers
    public static bool IsExpired(PluginUpgradeStats stats)
    {
        if (stats.buffDuration == -1)
        {
            Logger.LogError($"Expired check ran while buff duration was not set. \nbuffTimer should never be set without buffDuration");

        }

        return stats.buffTimer >= stats.buffDuration;
    }
    #endregion
}
