using System;
using Data.ScriptableObject;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class LvDryMassPatches
{
    internal static double GetLvDryMass(LaunchVehicleType lvType)
    {
        if (lvType == null)
            return 0.0;
        double ratio = ModConfig.GetLvDryMassRatio(lvType);
        if (ratio <= 0)
            return 0.0;
        return lvType.costLaunch * ratio;
    }

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
    [HarmonyPostfix]
    private static void CalculateCostStartPostfix(
        PMTabSchedule __instance,
        double fuelNoOnOrbit,
        ref double __result)
    {
        if (!ModConfig.LvDryMass)
            return;
        if (__result <= 0)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p?.LV == null || p.CargoAll?.cargoFuel == null)
            return;

        double dryMass = GetLvDryMass(p.LV.GetLaunchVehicleType()) * p.LVCount;
        if (dryMass <= 0)
            return;

        double num7;
        if (p.Start != p.StartHermesCase)
            num7 = p.AllFuelNeedNotOnPlanet + p.CargoAll.CargoCurrent;
        else
            num7 = p.GetMassToCalculateFuel() + p.CargoAll.cargoFuel.cargoMassPotencjal;
        num7 += fuelNoOnOrbit;

        if (num7 <= 0)
            return;

        __result = Math.Round(__result * (num7 + dryMass) / num7 * 10.0) / 10.0;
    }
}
