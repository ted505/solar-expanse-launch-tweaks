using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// Two fixes for solar sails (and orbit SCs in general):
///
/// 1. CalculateCostInFuel prefix: skip the self-launch dV block when
///    Start != StartHermesCase, meaning the SC is in orbit, not on the
///    surface.  Stock only guards on LV==null, so orbit SCs without an
///    LV get charged a phantom surface launch cost.
///
/// 2. FunctionCalculateFuel postfix: zero all fuel values for solar sails.
///    Stock computes Tsiolkovsky with exhaustV=1, producing garbage
///    minFuelCost.  Manual missions survive because the player's slider
///    stays at 0.  Cyclical missions break because ReduceFuelToMinimum
///    forces the slider to minFuelCost, then the game can't source fuel.
/// </summary>
[HarmonyPatch]
internal static class SolarSailPatches
{
    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void CalculateCostInFuelPrefix(PMTabSchedule __instance, ref double dV1, ref double dV2)
    {
        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null) return;
        if (PatchScope.IsAIMission(p)) return;

        if (p.Start != p.StartHermesCase)
        {
            dV1 = 0.0;
            dV2 = 0.0;
        }
    }

    [HarmonyPatch(typeof(PMTabSchedule), "FunctionCalculateFuel")]
    [HarmonyPostfix]
    private static void FunctionCalculateFuelPostfix(PMTabSchedule __instance)
    {
        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null) return;
        if (!p.SolarSC) return;
        if (PatchScope.IsAIMission(p)) return;

        p.SetFuelNeed(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        if (p.CargoAll?.cargoFuel != null)
            p.CargoAll.cargoFuel.cargoMassPotencjal = 0.0;
    }
}
