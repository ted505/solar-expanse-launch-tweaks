using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// Corrects the LV launch cost calculation for two cases:
///
/// 1. Our promoted Hermes (surface SC + LV with orbitFuelCredit):
///    The game's Hermes branch excludes SC dry mass from mass-to-lift.
///    We suppress the promotion so the normal formula runs, and set
///    fuelNoOnOrbit negative to subtract orbit-sourced fuel.
///    Result: num7 = SC_mass + cargo + (sliderFuel - orbitFuel).
///
/// 2. Game-native Hermes (orbit SC like Prometheus):
///    The game uses minFuelCost for fuelNoOnOrbit, which ignores extra
///    fuel on the slider. We replace it with (sliderFuel - orbitFuel)
///    so launch cost scales with the actual fuel payload.
/// </summary>
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
internal static class CostStartPatches
{
    [HarmonyPrefix]
    private static void Prefix(PMTabSchedule __instance, ref double fuelNoOnOrbit)
    {
        if (!ModConfig.OrbitFuelCredit)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null || p.Start == null || p.CargoAll == null)
            return;
        if (p.Start.objectTypes == Data.EObjectTypes.Orbit)
            return;
        if (p.Start == p.StartHermesCase)
            return;

        double sliderFuel = p.CargoAll.cargoFuel?.cargoMassPotencjal ?? 0.0;
        double orbitAvailable = p.StartHermesCaseDataCheckResources
            .CheckResourcesInterface(p.FuelNeedToStart);
        double fuelLvMustCarry = Math.Max(0.0, sliderFuel - orbitAvailable);

        // Distinguish our promotion from game-native Hermes (e.g. orbit SCs).
        OrbitFuelPatches.SuppressPromotion = true;
        bool isOurPromotion = (p.Start == p.StartHermesCase);
        OrbitFuelPatches.SuppressPromotion = false;

        if (isOurPromotion)
        {
            // Suppress so CalculateCostStart uses the normal formula
            // (includes SC dry mass). Negative offset removes orbit-sourced
            // fuel from the total.
            OrbitFuelPatches.SuppressPromotion = true;
            fuelNoOnOrbit = -Math.Min(sliderFuel, orbitAvailable);
        }
        else
        {
            // Game-native Hermes: don't suppress (SC is genuinely in orbit),
            // but fix fuelNoOnOrbit to use actual slider fuel, not minFuelCost.
            fuelNoOnOrbit = fuelLvMustCarry;
        }
    }

    [HarmonyFinalizer]
    private static void Finalizer()
    {
        OrbitFuelPatches.SuppressPromotion = false;
    }
}
