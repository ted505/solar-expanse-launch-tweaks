using System;
using Game;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Manager;

namespace LaunchFix.Patches;

/// <summary>
/// For non-orbit self-launches (e.g. Luna → Earth), the stock game
/// computes a fixed launch fuel cost based on (dryMass + minTransferFuel)
/// regardless of how much fuel is actually on the slider.  This means
/// extra fuel rides to orbit "for free."
///
/// This patch replaces that with a proper two-stage Tsiolkovsky:
///   Stage 1 (launch): burn orbV on full wet mass (dry + slider fuel)
///   Stage 2 (transfer): burn porkchop DV on remaining mass
/// Launch cost now scales with loaded fuel, matching the OrbitCase path.
/// </summary>
[HarmonyPatch]
internal static class SelfLaunchDvPatches
{
    private static bool _active;
    private static double _orbV;
    private static double _dV1;
    private static double _dV2;
    private static float _exhaustV;
    private static double _powBase;

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void CalculateCostInFuelPrefix(PMTabSchedule __instance, double dV1, double dV2)
    {
        _active = false;

        if (!ModConfig.SelfLaunchDv)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null || p.LV != null || p.SC == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;
        if (p.OrbitCase)
            return;
        if (p.Start == null || p.Start.objectTypes == Data.EObjectTypes.Orbit)
            return;

        var company = p.FlyCompany;
        if (company == null)
            return;

        float exhaustV = p.SC.GetTypeSpaceCraft().GetExhaustV(company);
        if (exhaustV <= 0f)
            return;

        double orbV = Math.Sqrt(6.67E-11 * p.Start.Mass / p.Start.Radius) / 1000.0;
        if (orbV <= 0)
            return;

        _orbV = orbV;
        _dV1 = dV1;
        _dV2 = dV2;
        _exhaustV = exhaustV;
        _powBase = MonoBehaviourSingleton<GameManager>.Instance.Economic.PowVariable;
        _active = true;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "SetTextTooltip")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void SetTextTooltipPrefix(
        PMTabSchedule __instance,
        ref double flightCost,
        ref double leftOverFuel,
        ref double launchCost)
    {
        if (!_active)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;

        ComputeTwoStage(p, out double launchFuel, out double transferFuel, out double leftOver);
        launchCost = launchFuel;
        flightCost = transferFuel;
        leftOverFuel = leftOver;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void CalculateCostInFuelPostfix(
        PMTabSchedule __instance,
        ref double costStart,
        ref double leftOverFuel,
        ref double _flightCost,
        ref double __result)
    {
        if (!_active)
            return;
        _active = false;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;

        ComputeTwoStage(p, out double launchFuel, out double transferFuel, out double leftOver);
        costStart = launchFuel;
        _flightCost = transferFuel;
        leftOverFuel = leftOver;
        __result = Math.Round((transferFuel + leftOver) * 10.0) / 10.0;
    }

    private static void ComputeTwoStage(
        PMMissionParameter p,
        out double launchFuel,
        out double transferFuel,
        out double leftOver)
    {
        double massToCalc = p.GetMassToCalculateFuel();
        double sliderFuel = p.CargoAll.cargoFuel.cargoMassPotencjal;
        double wetMass = massToCalc + sliderFuel;

        // Stage 1: surface to orbit
        double afterLaunch = wetMass * Math.Pow(_powBase, -_orbV / _exhaustV);
        launchFuel = wetMass - afterLaunch;

        // Stage 2: orbital transfer
        double transferDV = _dV1 + _dV2;
        double afterTransfer = afterLaunch * Math.Pow(_powBase, -transferDV / _exhaustV);
        transferFuel = afterLaunch - afterTransfer;

        leftOver = Math.Max(0.0, Math.Floor(afterTransfer - massToCalc));
    }
}
