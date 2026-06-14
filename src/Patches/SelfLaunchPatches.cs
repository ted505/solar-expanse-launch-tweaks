using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// Blocks self-launching spacecraft (e.g. Stratos from Mars) when the total
/// propellant needed (surface-to-orbit launch cost + interplanetary transfer
/// fuel) exceeds the spacecraft's fuel tank capacity.
///
/// The stock game computes a self-launch cost via the rocket equation and
/// stores it in AllFuelNeedLV, but never validates the sum against fuel
/// capacity — so a Stratos with 1 kt tanks can plan a mission needing 1.4 kt.
/// </summary>
[HarmonyPatch]
internal static class SelfLaunchPatches
{
    /// <summary>
    /// Returns the total propellant shortfall for a self-launching SC, or 0
    /// if the mission fits within the fuel tanks (or doesn't apply).
    /// Positive value = tons over capacity.
    /// </summary>
    internal static double GetSelfLaunchFuelShortfall(PMMissionParameter p)
    {
        if (PatchScope.IsAIMission(p)) return 0;
        if (p.LV != null) return 0;
        if (p.SC == null) return 0;
        if (p.CargoAll == null) return 0;

        double launchCost = p.AllFuelNeedLV;
        if (launchCost <= 0) return 0;

        double minTransferFuel = p.MINFuelCost;
        double fuelCapacity = p.SC.GetTypeSpaceCraft()
            .GetFuelCapacity(p.FlyCompany) * p.SCCount;

        double totalNeeded = launchCost + minTransferFuel;
        return Math.Max(0, totalNeeded - fuelCapacity);
    }

    /// <summary>
    /// The stock CheckSCNoLVFuelOk only checks MaxValueSliderFuel >= AllFuelNeed,
    /// which is trivially true because AllFuelNeed doesn't include the launch cost.
    /// We add: if launch cost + minimum transfer fuel > fuel capacity, block.
    /// </summary>
    [HarmonyPatch(typeof(PMMissionParameter), "CheckSCNoLVFuelOk")]
    [HarmonyPostfix]
    private static void CheckSCNoLVFuelOkPostfix(
        PMMissionParameter __instance, ref bool __result)
    {
        if (!ModConfig.SelfLaunchFuelCheck || !__result) return;
        if (PatchScope.IsAIMission(__instance)) return;

        if (GetSelfLaunchFuelShortfall(__instance) > 0)
            __result = false;
    }
}
