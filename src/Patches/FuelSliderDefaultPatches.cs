using System;
using Data.ScriptableObject;
using Game;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Elements.PlanMissionElements.PMScheduleElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using Manager;
using ScriptableObjectScripts;
using UnityEngine.UI;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(FuelSpaceCraftUI), nameof(FuelSpaceCraftUI.SetDate))]
internal static class FuelSliderDefaultPatches
{
    [HarmonyPostfix]
    private static void SetDatePostfix(
        FuelSpaceCraftUI __instance,
        Cargo _cargoFuel,
        SpacecraftType spacecraftType,
        ObjectInfoData objectInfoData,
        double fuelMinNeed,
        ResourceDefinition fuel,
        bool refreshUI,
        PMTabSchedule ___tabSchedule,
        Slider ___slider,
        ref double ___maxValueOld)
    {
        if (!ModConfig.FuelSliderDefault)
            return;

        try
        {
            var p = ___tabSchedule?.PlanMissionWindow?.PMMissionParameter;
            if (p == null || p.LV == null || p.CargoAll == null || p.SC == null)
                return;
            if (p.FlyCompany != MonoBehaviourSingleton<GameManager>.Instance.Player)
                return;
            if (p.ForCyclicalMission)
                return;
            if (p.MissionCreator != MissionInfo.EMissionCreator.Manual
                && p.MissionCreator != MissionInfo.EMissionCreator.DragAndDrop)
                return;
            if (p.StageWindow != PlanMissionWindow.EStageWindow.Schedule)
                return;
            if (p.ReduceFuelToMinimum || p.BlockAutoMaxValueFuel)
                return;
            if (_cargoFuel != p.CargoAll.cargoFuel)
                return;

            double fullTank = Math.Max(0.0, spacecraftType.GetFuelCapacity(p.FlyCompany) * p.SCCount);
            if (fullTank > ___slider.maxValue)
            {
                ___slider.maxValue = (float)Math.Round(fullTank);
                ___maxValueOld = Math.Round(fullTank);
            }

            double? fuelBudget = LaunchVehiclePatches.GetLvFuelBudget(p);
            if (!fuelBudget.HasValue)
                return;

            double defaultFuel = LaunchVehiclePatches.GetLvAwareFuelSliderLimit(p, fuelBudget.Value);
            defaultFuel = Math.Max(0.0, Math.Min(defaultFuel, fullTank));

            if (Math.Abs(__instance.SliderValue - defaultFuel) < 0.5)
                return;

            __instance.SliderSetValue((float)Math.Floor(defaultFuel));
        }
        catch
        {
            // Leave the stock slider behavior intact if planner state is not ready.
        }
    }
}
