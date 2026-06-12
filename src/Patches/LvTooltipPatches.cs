using System.Collections.Generic;
using Data.ScriptableObject;
using Extensions;
using Game;
using HarmonyLib;
using Language;
using Manager;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class LvTooltipPatches
{
    [HarmonyPatch(typeof(LaunchVehicleType), nameof(LaunchVehicleType.GetTooltipStats))]
    [HarmonyPostfix]
    private static void GetTooltipStatsPostfix(
        LaunchVehicleType __instance,
        ref List<(string, string)> __result)
    {
        if (!ModConfig.LvDryMass)
            return;

        double dryMass = LvDryMassPatches.GetLvDryMass(__instance);
        if (dryMass <= 0)
            return;

        string format = LEManager.Get("UI.MassFormat");
        string formatted = dryMass.ToPostfixString(format);
        __result.Insert(0, ("Dry Mass", formatted));
    }
}
