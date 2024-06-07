using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlantTimeModifier
{
    [BepInPlugin("com.utjan.PlantTimeModifier", "utjan.PlantTimeModifier", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        internal static ConfigEntry<bool> enabledPlugin;
        internal static ConfigEntry<float> timeMultiplierRepair;
        internal static ConfigEntry<float> timeMultiplierHide;
        internal static ConfigEntry<float> timeMultiplierProtect;

        private void Awake() //Awake() will run once when your plugin loads
        {
            enabledPlugin = Config.Bind(
                "Main Settings",
                "Enable Mod",
                true,
                new ConfigDescription("Enable timer multipliers")
            );

            timeMultiplierRepair = Config.Bind(
                "Main Settings",
                "Repair objective Time Multiplier",
                0.5f,
                new ConfigDescription("Multiplies the duration when doing 'Repairing objective' task action. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            );

            timeMultiplierHide = Config.Bind(
                "Main Settings",
                "Hide objective Time Multiplier",
                0.5f,
                new ConfigDescription("Multiplies the duration when doing 'Hiding objective' task action. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            );

            timeMultiplierProtect = Config.Bind(
                "Main Settings",
                "Protect objective Time Multiplier",
                0.5f,
                new ConfigDescription("Multiplies the time it takes to protect task objective. Like when placing a MS2000 marker. 0.5 = time is halved. 2.0 = time is doubled. 0 is instant", new AcceptableValueRange<float>(0, 5))
            );

            LogSource = Logger;

            new LeaveItemPatch().Enable();
            new BeaconPlantPatch().Enable();
        }
    }

    internal class LeaveItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GetActionsClass.Class1472), nameof(GetActionsClass.Class1472.method_0));
        }

        //Save list of objective zoneId's and plantTime to and make sure we're multiplying the base plantTime value on repeat actions
        static List<KeyValuePair<string, float>> LeaveItemList = new List<KeyValuePair<string, float>>(); //zoneId, plantTime

        [PatchPrefix]
        static void Prefix(GetActionsClass.Class1472 __instance)
        {
            if (!Plugin.enabledPlugin.Value)
                return;

            float plantTime;
            var pair = LeaveItemList.FirstOrDefault(p => p.Key == __instance.class1470_0.resultLeaveItem.zoneId);
            if (pair.Key != null)
            {
#if DEBUG
                Plugin.LogSource.LogWarning($"READING SAVED PLANTTIME {pair.Value} from zoneId {pair.Key}");
#endif
                plantTime = pair.Value;
            }
            else
            {
                LeaveItemList.Add(new KeyValuePair<string, float>(__instance.class1470_0.resultLeaveItem.zoneId, __instance.class1470_0.resultLeaveItem.plantTime));
                plantTime = __instance.class1470_0.resultLeaveItem.plantTime;
            }

#if DEBUG
            Plugin.LogSource.LogWarning($"BASE LEAVE ITEM TIME {__instance.class1470_0.resultLeaveItem.plantTime}");
            if (__instance.isMultitool)
                Plugin.LogSource.LogWarning($"REPAIRING OBJECTIVE DETECTED");
#endif

            float multiplier = (__instance.isMultitool == true) ? Plugin.timeMultiplierRepair.Value : Plugin.timeMultiplierHide.Value;

            __instance.class1470_0.resultLeaveItem.plantTime = plantTime * multiplier;

#if DEBUG
            Plugin.LogSource.LogWarning($"MODIFIED LEAVE ITEM TIME {__instance.class1470_0.resultLeaveItem.plantTime}");
#endif
        }
    }

    internal class BeaconPlantPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GetActionsClass.Class1473), nameof(GetActionsClass.Class1473.method_0));
        }

        //Save list of objective zoneId's and plantTime to and make sure we're multiplying the base plantTime value on repeat actions
        static List<KeyValuePair<string, float>> ResultBeaconList = new List<KeyValuePair<string, float>>(); //zoneId, plantTime

        [PatchPrefix]
        static void Prefix(GetActionsClass.Class1473 __instance)
        {
            if (!Plugin.enabledPlugin.Value)
                return;

            float plantTime;
            var pair = ResultBeaconList.FirstOrDefault(p => p.Key == __instance.resultBeacon.zoneId);
            if (pair.Key != null)
            {
#if DEBUG
                Plugin.LogSource.LogWarning($"READING SAVED PLANTTIME {pair.Value} from zoneId {pair.Key}");
#endif
                plantTime = pair.Value;
            }
            else
            {
                ResultBeaconList.Add(new KeyValuePair<string, float>(__instance.resultBeacon.zoneId, __instance.resultBeacon.plantTime));
                plantTime = __instance.resultBeacon.plantTime;
            }

#if DEBUG
            Plugin.LogSource.LogWarning($"BASE BEACON PLANT TIME {__instance.resultBeacon.plantTime}");
#endif

            __instance.resultBeacon.plantTime = plantTime * Plugin.timeMultiplierProtect.Value;

#if DEBUG
            Plugin.LogSource.LogWarning($"MODIFIED BEACON PLANT TIME {__instance.resultBeacon.plantTime}");
#endif
        }
    }
}
