using Data;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// When an LV launches an SC to the parent body's own low orbit, the
/// porkchop planner still assigns a non-zero delta-V for the "transfer."
/// The game then applies Tsiolkovsky to the SC's fuel budget, burning
/// propellant for a flight leg that doesn't exist — the LV delivers the
/// SC directly to the destination.  Zero out dV so the SC keeps all its
/// fuel.
/// </summary>
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
internal static class OrbitTransferPatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static void Prefix(PMTabSchedule __instance, ref double dV1, ref double dV2)
    {
        if (!ModConfig.LvOrbitTransfer)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p == null || p.LV == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;
        if (p.Start == null || p.Start.objectTypes == EObjectTypes.Orbit)
            return;
        if (p.Target == null || p.Start.LowOrbitCustom == null)
            return;
        if (p.Start.LowOrbitCustom.GetObjectInfo() != p.Target)
            return;

        dV1 = 0.0;
        dV2 = 0.0;
    }
}
