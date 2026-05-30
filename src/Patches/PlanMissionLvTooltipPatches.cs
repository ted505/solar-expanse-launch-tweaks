using System;
using Extensions;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Language;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(PMTab), "Update")]
internal static class PlanMissionLvTooltipPatches
{
    [HarmonyPostfix]
    private static void UpdatePostfix(PMTab __instance, ShowToolTip ___buttonNextShowToolTip)
    {
        if (__instance is not PMTabSelectLV || ___buttonNextShowToolTip == null)
            return;
        if (!__instance.Active || __instance.ButtonNextInteractable)
            return;

        string diagnostic = BuildLvPayloadDiagnostic(__instance.PlanMissionWindow?.PMMissionParameter);
        if (string.IsNullOrEmpty(diagnostic))
            return;

        string currentText = ___buttonNextShowToolTip.CustomTextFromCode ?? "";
        if (currentText.Contains("LV payload check:"))
            return;

        ___buttonNextShowToolTip.CustomTextFromCode = currentText + diagnostic;
    }

    private static string BuildLvPayloadDiagnostic(PMMissionParameter p)
    {
        if (p?.LV == null || p.SC == null || p.CargoAll == null)
            return "";

        try
        {
            string format = LEManager.Get("UI.MassFormat");
            double lvMax = p.LV.GetLaunchVehicleType()
                .MaxPayloadOnThisObject(p.Start, p.FlyCompany) * p.LVCount;

            double cargoOnly = p.CargoAll.CargoCurrent;
            double fuelCarriedByLv = LaunchVehiclePatches.GetFuelMassCarriedByLv(p);
            double lvPayload = LaunchVehiclePatches.GetLvPayloadMass(p);

            return "\n\n<color=#AAAAAA>LV payload check:"
                 + $"\nCargo-only load: {FormatMass(cargoOnly, format)} / {FormatMass(lvMax, format)}"
                 + $"\nLV-carried propellant: {FormatMass(fuelCarriedByLv, format)}"
                 + $"\nLV payload: {FormatMass(lvPayload, format)} / {FormatMass(lvMax, format)}"
                 + $" ({p.SCCount} SC, {p.LVCount} LV)</color>";
        }
        catch
        {
            return "";
        }
    }

    private static string FormatMass(double kg, string format)
    {
        if (double.IsNaN(kg) || double.IsInfinity(kg))
            return "N/A";
        return kg.ToPostfixString(format);
    }
}
