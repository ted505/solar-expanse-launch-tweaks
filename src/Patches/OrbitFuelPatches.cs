using Game.Info;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

[HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.StartHermesCase), MethodType.Getter)]
internal static class OrbitFuelPatches
{
    internal static bool SuppressPromotion;

    [HarmonyPostfix]
    private static void StartHermesCasePostfix(PMMissionParameter __instance, ref ObjectInfo __result)
    {
        if (!ModConfig.OrbitFuelCredit || SuppressPromotion)
            return;
        if (PatchScope.IsAIMission(__instance))
            return;
        if (__result != __instance.Start)
            return;
        if (__instance.LV == null)
            return;

        var stage = __instance.StageWindow;
        if (stage == Game.UI.Windows.Windows.PlanMissionWindow.EStageWindow.Cargo
            || stage == Game.UI.Windows.Windows.PlanMissionWindow.EStageWindow.CargoB)
            return;

        ObjectInfo start = __instance.Start;
        if (start == null || start.objectTypes == Data.EObjectTypes.Orbit)
            return;
        if (start.LowOrbitCustom == null)
            return;

        ObjectInfo lowOrbit = start.LowOrbitCustom.GetObjectInfo();
        if (lowOrbit == null)
            return;

        __result = lowOrbit;
    }
}
