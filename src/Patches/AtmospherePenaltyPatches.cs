using System;
using Data.ScriptableObject;
using Game;
using Game.Info;
using HarmonyLib;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(LaunchVehicleType), nameof(LaunchVehicleType.MaxPayloadOnThisObject))]
internal static class AtmospherePenaltyPatches
{
    [HarmonyPostfix]
    private static void MaxPayloadOnThisObjectPostfix(
        LaunchVehicleType __instance,
        ObjectInfo objectInfo,
        Company company,
        ref double __result)
    {
        if (!ModConfig.AtmospherePenalty)
            return;
        if (PatchScope.IsAICompany(company))
            return;
        if (double.IsNaN(__result) || __result <= 0)
            return;

        double factor = ModConfig.GetAtmospherePenaltyFactor(__instance);
        if (factor < 0)
            return;

        double pressure = objectInfo.HabitabilityParameters?.pressure ?? 0.0;
        if (pressure <= 0)
            return;

        double multiplier = Math.Max(0, 1.0 - (1.0 - factor) * pressure / 1.2);
        __result *= multiplier;
    }
}
