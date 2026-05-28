using Data.ScriptableObject;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class LaunchVehiclePatches
{
    [HarmonyPatch(typeof(LaunchVehicleType), nameof(LaunchVehicleType.CheckMaximumPayload))]
    [HarmonyPrefix]
    private static bool CheckMaximumPayloadPrefix(
        LaunchVehicleType __instance,
        CargoAll cargo,
        ISpacecraftInfo spacraft,
        ref bool __result)
    {
        double totalMass = cargo.CargoCurrent
                         + cargo.cargoFuel.cargoMassPotencjal
                         + (double)spacraft.GetMass();
        __result = (double)__instance.maxPayload >= totalMass;
        return false;
    }

    [HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.CheckLV))]
    [HarmonyPostfix]
    private static void CheckLVPostfix(PMMissionParameter __instance, ref bool __result)
    {
        if (!__result)
            return;

        var lv = __instance.LV;
        if (lv == null)
            return;

        var sc = __instance.SC;
        if (sc == null)
            return;

        var cargo = __instance.CargoAll;
        if (cargo == null)
            return;

        double maxPayload = lv.GetLaunchVehicleType()
            .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
            * __instance.LVCount;

        double totalMass = (double)(sc.GetMass() * __instance.SCCount)
                         + cargo.CargoCurrent
                         + cargo.cargoFuel.cargoMassPotencjal;

        if (totalMass > maxPayload)
            __result = false;
    }
}
