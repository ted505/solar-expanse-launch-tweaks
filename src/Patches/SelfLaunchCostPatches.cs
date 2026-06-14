using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// Fixes self-launch fuel bookkeeping. The game subtracts launch-leg
/// propellant (Tsiolkovsky) from the fuel budget but only charges the
/// abstracted CalculateCostStart value as costStart — the difference
/// vanishes. We set costStart = sliderFuel - fuelNeed so total fuel
/// charged equals the slider amount. The leftover (fuel delivered to
/// destination) is already correct; only the accounting changes.
/// </summary>
[HarmonyPatch]
internal static class SelfLaunchCostPatches
{
    private static bool _isSelfLaunch;
    private static double _launchProp;

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void CalculateCostInFuelPrefix()
    {
        _isSelfLaunch = false;
        _launchProp = 0.0;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    private static void CalculateCostStartPostfix(PMTabSchedule __instance)
    {
        if (!ModConfig.SelfLaunchCost)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null || p.LV != null || p.SC == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;
        if (p.Start == null || p.Start.objectTypes == Data.EObjectTypes.Orbit)
            return;

        _isSelfLaunch = true;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "SetTextTooltip")]
    [HarmonyPrefix]
    private static void SetTextTooltipPrefix(
        PMTabSchedule __instance,
        double flightCost,
        double leftOverFuel,
        ref double launchCost)
    {
        if (!_isSelfLaunch)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p?.CargoAll?.cargoFuel == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;

        double sliderFuel = p.CargoAll.cargoFuel.cargoMassPotencjal;
        _launchProp = Math.Max(0.0, sliderFuel - flightCost - leftOverFuel);
        launchCost = _launchProp;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    [HarmonyPostfix]
    private static void CalculateCostInFuelPostfix(ref double costStart)
    {
        if (!_isSelfLaunch)
            return;

        costStart = _launchProp;
        _isSelfLaunch = false;
    }
}
