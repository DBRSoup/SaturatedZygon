using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SaturatedZygon
{
    internal static class UpgradeHelper
    {
        internal static void RegisterKeyword(string key, string description)
        {

            var group = new TextBlocks.TextBlockGroup(-1)
            {
                blocks =
                [
                    new TextBlocks.TextBlock("", key),
                    new TextBlocks.TextBlock(description, key)
                ]
            };

            TextBlocks.strings[key] = group;


            var field = typeof(Keywords).GetField("keywords", BindingFlags.NonPublic | BindingFlags.Static);

            var dictionary = (Dictionary<string, Keywords.Keyword>)field.GetValue(null);

            dictionary[key] = new Keywords.Keyword
            {
                keys = [key],
                color = Color.white,
                colorHex = "#FFFFFF",
                descriptionID = key
            };
        }

        internal static IEnumerator LoadSoundAfterWwise( string stringID, Action<uint> onLoaded)
        {
            while (!AkUnitySoundEngine.IsInitialized())
                yield return null;

            uint soundID = AkUnitySoundEngine.GetIDFromString(stringID);

            ZygonPlugin.Logger.LogInfo($"Sound Loaded: {soundID}");
            onLoaded?.Invoke(soundID);
        }
        internal static Sprite LoadExternalIcon(string iconName, BaseUnityPlugin pluginInstance)
        {
            string path = Path.Combine(Path.GetDirectoryName(pluginInstance.Info.Location), iconName);
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                byte[] array = File.ReadAllBytes(path);
                Texture2D texture = new(2, 2);

                ImageConversion.LoadImage(texture, array);
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                ZygonPlugin.Logger.LogError("Icon Load Failed: " + ex.Message);
                return null;
            }
        }

        public static bool TryCreateGunUpgrade(
            string modGuid,
            string gearApiName,
            int upgradeId,
            string name,
            string description,
            Rarity rarity,
            UpgradeProperty[] properties,
            HexMap pattern,
            Upgrade.UpgradeFlags flags,
            Sprite icon,
            ManualLogSource log,
            out Upgrade upgrade,
            int priority = 0,
            Upgrade.CollectionSource collectionSource = Upgrade.CollectionSource.WorldPool)
        {
            upgrade = null;

            if (string.IsNullOrEmpty(modGuid))
            {
                log?.LogError("[UpgradeRegistration] modGuid is null/empty.");
                return false;
            }

            if (properties == null || properties.Length == 0)
            {
                log?.LogError("[UpgradeRegistration] properties array is null/empty.");
                return false;
            }

            IUpgradable gear = PlayerData.FindGear(gearApiName);
            if (gear == null)
            {
                log?.LogError($"[UpgradeRegistration] FindGear(\"{gearApiName}\") returned null. Is the API name correct?");
                return false;
            }

            // Character gear uses GenericPlayerUpgrade; everything else uses GenericGunUpgrade.
            // CreateUpgrade picks the subclass from upgradeParams.gear.
            if (gear is Character)
            {
                log?.LogWarning(
                    $"[UpgradeRegistration] Gear '{gearApiName}' is a Character. " +
                    "CreateUpgrade will build a GenericPlayerUpgrade — ensure your properties use Apply(Player, ...).");
            }

            PlayerData.CustomUpgradeParams upgradeParams = PlayerData.CustomUpgradeParams.Create(
                gear,
                upgradeId,
                name,
                description,
                rarity,
                icon);

            upgradeParams.flags = flags;
            upgradeParams.priority = priority;
            upgradeParams.collectionSource = collectionSource;
            upgradeParams.upgradeType = Upgrade.Type.Normal;
            upgradeParams.useDefaultUnlockCost = true;

            // Default Create() assigns Global.Instance.RarityPatterns[3]. Override when provided.
            if (pattern != null)
                upgradeParams.pattern = pattern;

            try
            {
                upgrade = PlayerData.CreateUpgrade(modGuid, upgradeParams, properties);
            }
            catch (Exception ex)
            {
                log?.LogError($"[UpgradeRegistration] CreateUpgrade threw: {ex}");
                upgrade = null;
                return false;
            }

            if (upgrade == null)
            {
                log?.LogError("[UpgradeRegistration] CreateUpgrade returned null.");
                return false;
            }

            // Belt-and-suspenders: ensure pattern stuck (CreateUpgrade already calls SetPattern).
            if (pattern != null)
                upgrade.SetPattern(pattern);

            return true;
        }

    }
}
