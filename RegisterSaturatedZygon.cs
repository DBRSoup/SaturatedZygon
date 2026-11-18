using BepInEx.Logging;
using System;

namespace SaturatedZygon
{
    internal static class RegisterSaturatedZygon
    {
        private static bool _registered;
        private static readonly string upgradeName = "Saturated Zygon";

        private static readonly string upgradeDescription = "Apply 10 stacks of <color=#ff6f2a>fire</color>, <color=#6be6ff>shock</color> and <color=#a8ff2e>acid</color> " +
                        "from a single non-disc weapon within 10 seconds to trigger <color=#ccffcc>saturation</color>, increasing <color=#c1ae8a>unattuned</color> damage. Killing with element damage adds reduced stacks.\n\n" +
                        "While Zygon Frenzy is equipped, elements no longer stack Saturated Zygon while <color=#ccffcc>saturation</color> or <color=#DC3132>frenzy</color> is active. " +
                        "Buffs also remain active during <color=#DC3132>frenzy</color>.";

        internal static void RegisterUpgrade()
        {
            if (_registered)
                return;

            ManualLogSource Logger = ZygonPlugin.Logger;
            int upgradeID = ZygonPlugin.upgradeID + 1;

            try
            {
                if (!UpgradeHelper.TryCreateGunUpgrade(
                        modGuid: ZygonPlugin.PLUGIN_GUID,
                        gearApiName: ZygonPlugin.gearApiName,
                        upgradeId: upgradeID,
                        name: upgradeName,
                        description: upgradeDescription,
                        rarity: Rarity.Exotic,
                        properties: [new UpgradeProperty_SaturatedZygon()],
                        pattern: CreatePattern(),
                        flags: Upgrade.UpgradeFlags.CanTurbocharge | Upgrade.UpgradeFlags.CanCleanseBeforeDiscovered,
                        icon: ZygonPlugin.SaturatedIcon,
                        log: ZygonPlugin.Logger,
                        out _))
                {
                    return;
                }

                _registered = true;
                Logger.LogInfo(
                    $"Registered {upgradeName} on gear {ZygonPlugin.gearApiName} " +
                    $"(id={upgradeID}).");

                //// Optional: grant one unlocked instance for local testing.
                //// Uncomment while iterating, then remove / gate behind config before shipping.
                ////
                //var gear = PlayerData.FindGear("photondisc");
                //if (gear != null && CustomUpgradeInstance != null)
                //{
                //    var instance = PlayerData.CollectInstance(gear, CustomUpgradeInstance, PlayerData.UnlockFlags.Hidden);
                //    instance?.Unlock(quiet: true);
                //    Logger.LogInfo("[UpgradeTemplate] Granted a test instance (Hidden unlock).");
                //}
            }
            catch (Exception ex)
            {
                Logger.LogError($"Upgrade Registration failed: {ex}");
            }
        }

        public static HexMap CreatePattern()
        {
            HexMap map = new(2, 4);

            map[1, 0].enabled = true;
            map[1, 0].connections = HexMap.Direction.South;
            map[1, 1].enabled = true;
            map[1, 1].connections = HexMap.Direction.South | HexMap.Direction.SouthWest;
            map[1, 2].enabled = true;
            map[1, 2].connections = HexMap.Direction.SouthWest;
            map[0, 3].enabled = true;
            map[0, 3].connections = HexMap.Direction.North;
            map[0, 2].enabled = true;

            return map;
        }

    }


}
