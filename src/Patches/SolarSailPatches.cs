using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// Two fixes for solar sails (and orbit SCs in general):
///
/// 1. CalculateCostInFuel prefix: when the craft departs from orbit rather
///    than the surface (Start != StartHermesCase), zero the porkchop
///    dV1/dV2.  Pre-existing behavior for self-launching (LV == null)
///    orbit SCs; left unchanged here.
///
///    Added guard (LV != null -> return): OrbitFuelPatches promotes a
///    surface SC's StartHermesCase to low orbit on any LV launch, which
///    also makes Start != StartHermesCase true.  Without this guard the
///    prefix zeroed the real interplanetary transfer dV1/dV2, collapsing
///    the SC's transfer-leg cost to 0 (task 005).  With an LV the ascent
///    is billed separately via CalculateCostStart, so the transfer must
///    remain billed.
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
        if (p.LV != null) return;

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
