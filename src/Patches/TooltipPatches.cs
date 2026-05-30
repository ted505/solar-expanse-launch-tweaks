using System;
using Extensions;
using Game;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Language;
using Manager;

namespace LaunchFix.Patches;

/// <summary>
/// Appends concrete numbers (e.g. "120 t / 15 t") to each blocker line
/// shown when the Fly button is greyed out.
/// </summary>
[HarmonyPatch]
internal static class TooltipPatches
{
    [HarmonyPatch(typeof(PMTabSchedule), nameof(PMTabSchedule.GetTextToltip))]
    [HarmonyPostfix]
    private static void GetTextToltipPostfix(PMMissionParameter PMMissionParameter, ref string __result)
    {
        if (PMMissionParameter == null)
            return;

        string massFormat = LEManager.Get("UI.MassFormat");
        string extra = "";

        // --- LV payload overload ---
        if (!PMMissionParameter.CheckLvOk && PMMissionParameter.LV != null && PMMissionParameter.SC != null && PMMissionParameter.CargoAll != null)
        {
            try
            {
                double maxPayload = PMMissionParameter.LV.GetLaunchVehicleType()
                    .MaxPayloadOnThisObject(PMMissionParameter.Start, PMMissionParameter.FlyCompany)
                    * PMMissionParameter.LVCount;

                double totalMass = LaunchVehiclePatches.GetLvPayloadMass(PMMissionParameter);
                double fuelCarriedByLv = LaunchVehiclePatches.GetFuelMassCarriedByLv(PMMissionParameter);

                extra += $"\n<color=#AAAAAA>Payload: {FormatMass(totalMass, massFormat)} / {FormatMass(maxPayload, massFormat)} capacity</color>";
                extra += $"\n<color=#AAAAAA>LV-carried propellant: {FormatMass(fuelCarriedByLv, massFormat)}</color>";
            }
            catch { }
        }

        // --- Insufficient thrust ---
        if (!PMMissionParameter.ThrustOk && PMMissionParameter.SC != null && PMMissionParameter.CargoAll != null)
        {
            try
            {
                double accel = PMMissionParameter.GetAcceleration(); // km/s²
                double dv = 0.0;
                if (PMMissionParameter.DV11.HasValue && PMMissionParameter.DV22.HasValue)
                    dv = PMMissionParameter.DV11.Value + PMMissionParameter.DV22.Value;

                float dvMult = MonoBehaviourSingleton<GameManager>.Instance.Economic.DeltaVMultiplayerCheckingThrust;
                double burnTimeNeeded = dv * 1000.0 * dvMult / accel; // seconds
                double missionTime = PMMissionParameter.TimeSpanMissionLenght.TotalSeconds;

                string burnStr = FormatDuration(burnTimeNeeded);
                string missionStr = FormatDuration(missionTime);

                float thrustKn = PMMissionParameter.SC.GetTypeSpaceCraft().GetThrust(PMMissionParameter.FlyCompany) * PMMissionParameter.SCCount;
                double wetMass = (double)PMMissionParameter.SC.GetMass()
                               + PMMissionParameter.CargoAll.CargoCurrent
                               + PMMissionParameter.CargoAll.cargoFuel.cargoMassPotencjal;

                extra += $"\n<color=#AAAAAA>Burn time: {burnStr} / {missionStr} available"
                       + $"\nThrust {thrustKn.ToPostfixString(LEManager.Get("UI.ForceFormat"))} · Wet mass {FormatMass(wetMass, massFormat)}</color>";
            }
            catch { }
        }

        // --- Fuel capacity exceeded ---
        if (!PMMissionParameter.MAXCapacityFuelOk && !PMMissionParameter.CheckMaxCapacityFuelOkTooltip()
            && PMMissionParameter.SC != null && PMMissionParameter.CargoAll != null)
        {
            try
            {
                double fuelLoaded = PMMissionParameter.CargoAll.cargoFuel.cargoMassPotencjal;
                double fuelMax = PMMissionParameter.MaxValueSliderFuel();
                double fuelMin = PMMissionParameter.MINFuelCost;

                if (fuelLoaded > fuelMax)
                    extra += $"\n<color=#AAAAAA>Fuel: {FormatMass(fuelLoaded, massFormat)} / {FormatMass(fuelMax, massFormat)} tank capacity</color>";
                else if (fuelLoaded < fuelMin)
                    extra += $"\n<color=#AAAAAA>Fuel: {FormatMass(fuelLoaded, massFormat)} / {FormatMass(fuelMin, massFormat)} minimum needed</color>";
            }
            catch { }
        }

        // --- Insufficient fuel at start ---
        if (!PMMissionParameter.RemoveFuelOk && PMMissionParameter.CargoAll != null)
        {
            try
            {
                double fuelNeeded = PMMissionParameter.AllFuelNeed;
                extra += $"\n<color=#AAAAAA>Fuel required: {FormatMass(fuelNeeded, massFormat)}</color>";
            }
            catch { }
        }

        // --- Self-launch fuel exceeds tank capacity ---
        if (!PMMissionParameter.ScNoLVFuelOk && PMMissionParameter.SC != null && PMMissionParameter.LV == null)
        {
            try
            {
                double launchCost = PMMissionParameter.AllFuelNeedLV;
                double minTransfer = PMMissionParameter.MINFuelCost;
                double fuelCap = PMMissionParameter.SC.GetTypeSpaceCraft()
                    .GetFuelCapacity(PMMissionParameter.FlyCompany) * PMMissionParameter.SCCount;

                if (launchCost > 0 && launchCost + minTransfer > fuelCap)
                {
                    extra += $"\n<color=#AAAAAA>Launch: {FormatMass(launchCost, massFormat)}"
                           + $" + Transfer: {FormatMass(minTransfer, massFormat)}"
                           + $" = {FormatMass(launchCost + minTransfer, massFormat)}"
                           + $" / {FormatMass(fuelCap, massFormat)} tank capacity</color>";
                }
            }
            catch { }
        }

        // --- Life support ---
        if (!PMMissionParameter.LifeSupportOk && PMMissionParameter.SC != null)
        {
            try
            {
                double missionDays = PMMissionParameter.TimeSpanMissionLenght.TotalDays;
                float maxSupply = PMMissionParameter.SC.GetTypeSpaceCraft()
                    .GetMAXLifeSupport(PMMissionParameter.FlyCompany);
                extra += $"\n<color=#AAAAAA>Supplies: {maxSupply.ToPostfixString(massFormat)} for {Math.Round(missionDays)} day mission</color>";
            }
            catch { }
        }

        if (extra.Length > 0)
            __result += extra;
    }

    private static string FormatMass(double kg, string format)
    {
        return kg.ToPostfixString(format);
    }

    private static string FormatDuration(double totalSeconds)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds))
            return "N/A";
        var ts = TimeSpan.FromSeconds(Math.Abs(totalSeconds));
        if (ts.TotalDays >= 1.0)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1.0)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
    }
}
