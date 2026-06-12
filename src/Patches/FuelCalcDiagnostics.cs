using System;
using Game;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(PMTabSchedule), "FunctionCalculateFuel")]
internal static class FuelCalcDiagnostics
{
    [HarmonyPostfix]
    private static void Postfix(PMTabSchedule __instance)
    {
        try
        {
            var p = __instance.PlanMissionWindow?.PMMissionParameter;
            if (p == null)
            {
                Debug.Log("[LaunchFix:DIAG] PMMissionParameter is null");
                return;
            }

            string startName = p.Start?.ObjectName ?? "null";
            string targetName = p.Target?.ObjectName ?? "null";
            string startType = p.Start?.objectTypes.ToString() ?? "null";
            string targetType = p.Target?.objectTypes.ToString() ?? "null";
            string hermesName = "?";
            bool hermesMatch = false;
            try
            {
                var hermes = p.StartHermesCase;
                hermesName = hermes?.ObjectName ?? "null";
                hermesMatch = p.Start == hermes;
            }
            catch (Exception ex)
            {
                hermesName = $"ERR:{ex.GetType().Name}";
            }

            string lvName = p.LV?.GetLaunchVehicleType()?.ID ?? "none";
            string scName = p.SC?.GetTypeSpaceCraft()?.ID ?? "none";

            double sliderFuel = p.CargoAll?.cargoFuel?.cargoMassPotencjal ?? -1;
            double massToCalc = p.GetMassToCalculateFuel();
            bool orbitCase = p.OrbitCase;

            double dv1 = p.DV11 ?? 0;
            double dv2 = p.DV22 ?? 0;

            float exhaustV = 0f;
            if (p.SC != null && p.FlyCompany != null)
                exhaustV = p.SC.GetTypeSpaceCraft().GetExhaustV(p.FlyCompany);

            double powBase = MonoBehaviourSingleton<GameManager>.Instance.Economic.PowVariable;

            double num6 = massToCalc + sliderFuel;
            double num8 = (0.0 - (dv1 + dv2)) / exhaustV;
            double num9 = Math.Pow(powBase, num8);
            double num10 = num6 * num9;
            double recomputedLeftover = num10 - massToCalc;

            Debug.Log($"[LaunchFix:DIAG] === FunctionCalculateFuel ===");
            Debug.Log($"[LaunchFix:DIAG] Route: {startName} ({startType}) -> {targetName} ({targetType})");
            Debug.Log($"[LaunchFix:DIAG] LV={lvName}  SC={scName}  OrbitCase={orbitCase}");
            Debug.Log($"[LaunchFix:DIAG] DV11={dv1:F4}  DV22={dv2:F4}  exhaustV={exhaustV:F4}  powBase={powBase:F6}");
            Debug.Log($"[LaunchFix:DIAG] massToCalcFuel={massToCalc:F2}  sliderFuel={sliderFuel:F2}");
            Debug.Log($"[LaunchFix:DIAG] num6(wetMass)={num6:F2}  num9(tsiolRatio)={num9:F6}  num10(afterBurn)={num10:F2}");
            Debug.Log($"[LaunchFix:DIAG] recomputedLeftover={recomputedLeftover:F2}");
            Debug.Log($"[LaunchFix:DIAG] STORED: leftOver={p.LeftOverFuel:F2}  flightCost={p.FlightCost:F2}  costLV={p.AllFuelNeedLV:F2}  minFuel={p.MINFuelCost:F2}  fuelToOrbit={p.FuelNeedToGetFuelToOrbit:F2}  allFuelNeed={p.AllFuelNeed:F2}");
            Debug.Log($"[LaunchFix:DIAG] StartHermesCase={hermesName}  Start==Hermes={hermesMatch}");

            if (p.Start?.LowOrbitCustom != null)
            {
                var lo = p.Start.LowOrbitCustom.GetObjectInfo();
                string lowOrbitName = lo?.ObjectName ?? "null";
                bool targetIsOwnOrbit = lo == p.Target;
                Debug.Log($"[LaunchFix:DIAG] Start.LowOrbit={lowOrbitName}  Target==Start.LowOrbit={targetIsOwnOrbit}");
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[LaunchFix:DIAG] EXCEPTION: {ex}");
        }
    }
}
