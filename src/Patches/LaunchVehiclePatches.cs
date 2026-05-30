using System;
using Data.ScriptableObject;
using Game;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using Manager;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class LaunchVehiclePatches
{
    /// <summary>
    /// Returns the maximum fuel mass the LV can carry after accounting for
    /// SC dry mass and cargo, or null if no LV is selected.
    /// </summary>
    internal static double? GetLvFuelBudget(PMMissionParameter p)
    {
        var lv = p.LV;
        if (lv == null) return null;
        var sc = p.SC;
        if (sc == null) return null;
        var cargo = p.CargoAll;
        if (cargo == null) return null;

        double maxPayload = lv.GetLaunchVehicleType()
            .MaxPayloadOnThisObject(p.Start, p.FlyCompany)
            * p.LVCount;

        double nonFuelMass = GetLvNonFuelPayloadMass(p);

        return Math.Max(0.0, maxPayload - nonFuelMass);
    }

    internal static double GetLvPayloadMass(PMMissionParameter p)
    {
        if (p == null || p.CargoAll == null)
            return 0.0;

        return GetLvNonFuelPayloadMass(p) + GetFuelMassCarriedByLv(p);
    }

    internal static double GetLvNonFuelPayloadMass(PMMissionParameter p)
    {
        if (p == null || p.SC == null || p.CargoAll == null)
            return 0.0;

        double payload = p.CargoAll.CargoCurrent;

        // Hermes case: the spacecraft is already at the orbital staging point.
        // The LV lifts cargo/modules and any missing propellant, not the SV dry mass.
        if (p.Start != p.StartHermesCase)
            return payload;

        return payload + (double)(p.SC.GetMass() * p.SCCount);
    }

    internal static double GetFuelMassCarriedByLv(PMMissionParameter p)
    {
        if (p == null || p.CargoAll == null)
            return 0.0;

        if (p.Start != p.StartHermesCase)
        {
            double fuelAlreadyStaged = p.StartHermesCaseDataCheckResources
                .CheckResourcesInterface(p.FuelNeedToStart);
            return Math.Max(0.0, p.FuelNeedToGetFuelToOrbit - fuelAlreadyStaged);
        }

        return p.CargoAll.cargoFuel?.cargoMassPotencjal ?? 0.0;
    }

    internal static double GetLvAwareFuelSliderLimit(PMMissionParameter p, double fuelBudget)
    {
        if (p == null)
            return fuelBudget;

        if (p.Start != p.StartHermesCase)
        {
            double fuelAlreadyStaged = p.StartHermesCaseDataCheckResources
                .CheckResourcesInterface(p.FuelNeedToStart);
            return fuelAlreadyStaged + fuelBudget;
        }

        return fuelBudget;
    }

    [HarmonyPatch(typeof(LaunchVehicleType), nameof(LaunchVehicleType.CheckMaximumPayload))]
    [HarmonyPrefix]
    private static bool CheckMaximumPayloadPrefix(
        LaunchVehicleType __instance,
        CargoAll cargo,
        ISpacecraftInfo spacraft,
        ref bool __result)
    {
        double totalMass = cargo.CargoCurrent
                         + cargo.cargoFuel.cargoMassPotencjal
                         + (double)spacraft.GetMass();
        __result = (double)__instance.maxPayload >= totalMass;
        return false;
    }

    [HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.CheckLV))]
    [HarmonyPostfix]
    private static void CheckLVPostfix(PMMissionParameter __instance, ref bool __result)
    {
        if (!__result)
            return;

        if (__instance.StageWindow == PlanMissionWindow.EStageWindow.SelectLaunchVehicle
            || __instance.StageWindow == PlanMissionWindow.EStageWindow.SelectLaunchVehicleB)
        {
            return;
        }

        double? fuelBudget = GetLvFuelBudget(__instance);
        if (!fuelBudget.HasValue)
            return;

        double fuelCarriedByLv = GetFuelMassCarriedByLv(__instance);
        if (fuelCarriedByLv > fuelBudget.Value)
            __result = false;
    }

    /// <summary>
    /// Caps the fuel slider maximum so the total wet mass cannot exceed
    /// the LV's payload capacity. Only applies to code-driven flights
    /// (AI companies and player cyclical missions) so that the player's
    /// manual fuel slider still shows full SC tank capacity — the CheckLV
    /// postfix and tooltip handle the player-facing enforcement instead.
    /// When minFuelCost exceeds the capped max, FuelMinNeedIsLargerThanMaxValue
    /// is set, which is the game's standard "impossible mission" signal.
    /// </summary>
    [HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.MaxValueSliderFuel))]
    [HarmonyPostfix]
    private static void MaxValueSliderFuelPostfix(PMMissionParameter __instance, ref double __result)
    {
        bool isPlayerManual = __instance.FlyCompany == MonoBehaviourSingleton<GameManager>.Instance.Player
                           && !__instance.ReduceFuelToMinimum;
        if (isPlayerManual)
            return;

        double? fuelBudget = GetLvFuelBudget(__instance);
        if (!fuelBudget.HasValue)
            return;

        double limit = GetLvAwareFuelSliderLimit(__instance, fuelBudget.Value);
        if (__result > limit)
            __result = limit;
    }
}
