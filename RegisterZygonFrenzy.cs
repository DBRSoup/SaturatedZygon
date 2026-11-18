using BepInEx.Logging;
using System;

namespace SaturatedZygon
{
    internal static class RegisterZygonFrenzy
    {
        private static bool _registered;
        private static readonly string upgradeName = "Zygon Frenzy";

        //private static readonly string upgradeDescription = "While Saturated Zygon is equipped, stack elements with discs to enter a <color=#DC3132>frenzy</color> for additional damage. " +
        //                "Discs only add stacks during <link=wane><color=#9370db>waning</color></link>.\n\n" +
        //                "Elements no longer stack Saturated Zygon while <color=#ccffcc>saturation</color> or <color=#DC3132>frenzy</color> is active.";

        private static readonly string upgradeDescription = "Apply 10 stacks of <color=#ff6f2a>fire</color>, <color=#6be6ff>shock</color> and <color=#a8ff2e>acid</color> " +
                        "with discs within 10 seconds to trigger <color=#DC3132>frenzy</color>, increasing <color=#c1ae8a>unattuned</color> damage. <color=#DC3132>Frenzy</color> cannot retrigger while a Zygon buff is active unless <link=wane><color=#9370db>waning</color></link>.\n\n" +
                        "While Saturated Zygon is equipped, discs no longer stack Zygon Frenzy outside of <color=#9370db>waning</color> and it now gains stacks from kills with element damage.";

        internal static void RegisterUpgrade()
        {
            if (_registered)
                return;

            ManualLogSource Logger = ZygonPlugin.Logger;
            int upgradeID = ZygonPlugin.upgradeID + 2;

            try
            {
                if (!UpgradeHelper.TryCreateGunUpgrade(
                        modGuid: ZygonPlugin.PLUGIN_GUID,
                        gearApiName: ZygonPlugin.gearApiName,
                        upgradeId: upgradeID,
                        name: upgradeName,
                        description: upgradeDescription,
                        rarity: Rarity.Exotic,
                        properties: [new UpgradeProperty_ZygonFrenzy()],
                        pattern: CreatePattern(),
                        flags: Upgrade.UpgradeFlags.CanTurbocharge | Upgrade.UpgradeFlags.CanCleanseBeforeDiscovered,
                        icon: ZygonPlugin.FrenzyIcon,
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
            HexMap map = new(3, 3);

            map[0, 2].enabled = true;
            map[0, 2].connections = HexMap.Direction.North;
            map[0, 1].enabled = true;
            map[0, 1].connections = HexMap.Direction.NorthEast;
            map[1, 0].enabled = true;
            map[1, 0].connections = HexMap.Direction.SouthEast;
            map[2, 1].enabled = true;
            map[2, 1].connections = HexMap.Direction.South;
            map[2, 2].enabled = true;

            return map;
        }

    }


}
