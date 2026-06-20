using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Manager;

namespace LaunchFix.Patches;

/// Stock CalculateCostStart applies surface gravity to LV launch cost twice: num5 (max
/// payload) is divided by gravity via ConvertPayloadToFinalG, and the cost is then
/// multiplied by num3 (the surface-gravity ratio), so cost scales with gravity squared.
/// Gravity should apply once. Divide __result by num3 to undo the redundant factor; on the
/// home body num3 == 1, so this is a no-op there.
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
internal static class LvGravityCostPatches
{
    [HarmonyPostfix]
    private static void CalculateCostStartPostfix(PMTabSchedule __instance, ref double __result)
    {
        if (!ModConfig.LvGravityCost)
            return;
        if (__result <= 0)
            return;

        var p = __instance.PlanMissionWindow?.PMMissionParameter;
        if (p?.LV == null || p.Start == null)
            return;
        if (PatchScope.IsAIMission(p))
            return;

        var main = MonoBehaviourSingleton<GameManager>.Instance?.Player?.mainObjectInfo;
        if (main == null)
            return;

        double num1 = p.Start.Mass / main.Mass;
        double num2 = Math.Pow(p.Start.Radius / main.Radius, 2.0);
        if (Math.Abs(num2) < 9.9999997473787516E-06)
            return;
        double num3 = num1 / num2;
        if (num3 <= 1e-9)
            return;

        __result = Math.Round(__result / num3 * 10.0) / 10.0;
    }
}
