using System;
using Extensions;
using Game;
using Game.Info;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Elements.PlanMissionElements.PMScheduleElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using Language;
using Manager;
using TMPro;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class SupplyMassPatches
{
    private static readonly System.Reflection.MethodInfo FunctionCalculateFuel =
        AccessTools.Method(typeof(PMTabSchedule), "FunctionCalculateFuel");

    private static bool recalculatingFuel;

    internal static bool ShouldCountSliderSupplyMass(PMMissionParameter p)
    {
        if (p == null || p.CargoAll?.cargoFuel == null)
            return false;
        if (p.FlyCompany != MonoBehaviourSingleton<GameManager>.Instance.Player)
            return false;
        if (p.ForCyclicalMission)
            return false;
        if (p.MissionCreator != MissionInfo.EMissionCreator.Manual
            && p.MissionCreator != MissionInfo.EMissionCreator.DragAndDrop)
            return false;
        if (p.StageWindow != PlanMissionWindow.EStageWindow.Schedule)
            return false;
        return p.CargoAll.cargoFuel.lifeSupportValue > 0.0;
    }

    internal static double GetSliderSupplyMass(PMMissionParameter p)
    {
        if (!ShouldCountSliderSupplyMass(p))
            return 0.0;

        return Math.Max(0.0,
            p.CargoAll.cargoFuel.lifeSupportValue
            / MonoBehaviourSingleton<GameManager>.Instance.Economic.SupplyToLifeSupportMultiplayer);
    }

    [HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.GetMassToCalculateFuel))]
    [HarmonyPostfix]
    private static void GetMassToCalculateFuelPostfix(PMMissionParameter __instance, ref double __result)
    {
        if (!ModConfig.SupplyMassInFuel)
            return;

        __result += GetSliderSupplyMass(__instance);
    }

    [HarmonyPatch(typeof(PMLifeSupport), "LifeValueSupportChange")]
    [HarmonyPostfix]
    private static void LifeValueSupportChangePostfix(PlanMissionWindow ___planMissionWindow, TextMeshProUGUI ___time)
    {
        if (!ModConfig.SupplyMassInFuel)
            return;

        bool setGuard = false;
        try
        {
            var p = ___planMissionWindow?.PMMissionParameter;
            if (!ShouldCountSliderSupplyMass(p) || ___time == null)
                return;

            string format = LEManager.Get("UI.MassFormat");
            string supplyMass = GetSliderSupplyMass(p).ToPostfixString(format);
            ___time.text = $"{supplyMass} | {___time.text}";

            if (recalculatingFuel || FunctionCalculateFuel == null)
                return;

            recalculatingFuel = true;
            setGuard = true;
            FunctionCalculateFuel.Invoke(___planMissionWindow.PmTabSchedule, new object[] { true });
        }
        catch
        {
            // Leave the stock life-support label intact if UI state is not ready.
        }
        finally
        {
            if (setGuard)
                recalculatingFuel = false;
        }
    }
}
