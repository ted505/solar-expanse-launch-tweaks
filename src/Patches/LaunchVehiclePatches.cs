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
        if (PatchScope.IsAIMission(p)) return null;
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
        if (PatchScope.IsAIMission(p)) return 0.0;
        if (p == null || p.CargoAll == null)
            return 0.0;

        return GetLvNonFuelPayloadMass(p) + GetFuelMassCarriedByLv(p);
    }

    internal static double GetLvNonFuelPayloadMass(PMMissionParameter p)
    {
        if (PatchScope.IsAIMission(p)) return 0.0;
        if (p == null || p.SC == null || p.CargoAll == null)
            return 0.0;

        double payload = p.CargoAll.CargoCurrent + SupplyMassPatches.GetSliderSupplyMass(p);

        if (p.LV != null && ModConfig.LvDryMass)
            payload += LvDryMassPatches.GetLvDryMass(p.LV.GetLaunchVehicleType()) * p.LVCount;

        // The LV must lift SC dry mass only if the SC is physically on the
        // surface.  Orbit SCs (e.g. Nike) are already in orbit — Start is
        // the surface body where the LV launches, not where the SC sits.
        if (p.Start != null && p.Start.objectTypes != Data.EObjectTypes.Orbit
            && !p.SC.GetTypeSpaceCraft().OrbitSC)
            payload += (double)(p.SC.GetMass() * p.SCCount);

        return payload;
    }

    internal static double GetFuelMassCarriedByLv(PMMissionParameter p)
    {
        if (PatchScope.IsAIMission(p)) return 0.0;
        if (p == null || p.CargoAll == null)
            return 0.0;

        if (p.Start != p.StartHermesCase)
        {
            double totalFuel = p.CargoAll.cargoFuel?.cargoMassPotencjal ?? 0.0;
            double fuelAlreadyStaged = p.StartHermesCaseDataCheckResources
                .CheckResourcesInterface(p.FuelNeedToStart);
            return Math.Max(0.0, totalFuel - fuelAlreadyStaged);
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
        if (!ModConfig.LvPayloadCheck)
            return true;
        if (spacraft != null && PatchScope.IsAICompany(spacraft.GetCompany()))
            return true;

        double totalMass = cargo.CargoCurrent
                         + cargo.cargoFuel.cargoMassPotencjal;
        if (!spacraft.GetTypeSpaceCraft().OrbitSC)
            totalMass += (double)spacraft.GetMass();
        if (ModConfig.LvDryMass)
            totalMass += LvDryMassPatches.GetLvDryMass(__instance);
        __result = (double)__instance.maxPayload >= totalMass;
        return false;
    }

    [HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.CheckLV))]
    [HarmonyPostfix]
    private static void CheckLVPostfix(PMMissionParameter __instance, ref bool __result)
    {
        if (!ModConfig.LvPayloadCheck || !__result)
            return;
        if (PatchScope.IsAIMission(__instance))
            return;

        if (__instance.StageWindow == PlanMissionWindow.EStageWindow.SelectLaunchVehicle
            || __instance.StageWindow == PlanMissionWindow.EStageWindow.SelectLaunchVehicleB)
        {
            return;
        }

        if (__instance.LV == null)
            return;

        double maxPayload = __instance.LV.GetLaunchVehicleType()
            .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
            * __instance.LVCount;
        double totalPayload = GetLvNonFuelPayloadMass(__instance) + GetFuelMassCarriedByLv(__instance);
        if (totalPayload > maxPayload)
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
        if (!ModConfig.LvPayloadCheck)
            return;
        if (PatchScope.IsAIMission(__instance))
            return;

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
