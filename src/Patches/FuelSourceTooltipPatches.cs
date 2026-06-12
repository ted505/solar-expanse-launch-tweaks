using System;
using System.Reflection;
using Extensions;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Elements.PlanMissionElements.PMScheduleElements;
using HarmonyLib;
using Language;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(PMTabSchedule), "SetTextTooltip")]
internal static class FuelSourceTooltipPatches
{
    private static readonly FieldInfo PmLabelsField =
        AccessTools.Field(typeof(PMTabSchedule), "pmLabels");

    private static readonly FieldInfo ShowToolTipField =
        AccessTools.Field(typeof(PMLabels), "showToolTipEstimatedFuel");

    [HarmonyPostfix]
    private static void Postfix(PMTabSchedule __instance)
    {
        if (!ModConfig.OrbitFuelCredit)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null || p.Start == null || p.CargoAll == null)
            return;
        if (p.Start == p.StartHermesCase)
            return;

        var pmLabels = (PMLabels)PmLabelsField?.GetValue(__instance);
        if (pmLabels == null)
            return;

        var showToolTip = (ShowToolTip)ShowToolTipField?.GetValue(pmLabels);
        if (showToolTip == null)
            return;

        double sliderFuel = p.CargoAll.cargoFuel?.cargoMassPotencjal ?? 0.0;
        double orbitAvailable = p.StartHermesCaseDataCheckResources
            .CheckResourcesInterface(p.FuelNeedToStart);
        double fromOrbit = Math.Min(sliderFuel, orbitAvailable);
        double fromLv = Math.Max(0.0, sliderFuel - fromOrbit);

        if (fromOrbit <= 0.0)
            return;

        string s = LEManager.Get("UI.MassFormat");
        string fuelIcon = p.FuelNeedToStart.GetText(longText: false);

        string extra = Environment.NewLine
            + "<color=#88CCFF>Orbit-staged: " + fuelIcon + " "
            + s.MyFormat(fromOrbit.ToPostfixString(), "") + "</color>";

        if (fromLv > 0.0)
        {
            extra += Environment.NewLine
                + "<color=#FFCC88>LV-carried: " + fuelIcon + " "
                + s.MyFormat(fromLv.ToPostfixString(), "") + "</color>";
        }

        showToolTip.CustomTextFromCode += extra;
    }
}
