using System.Collections.Generic;
using Data.ScriptableObject;
using Extensions;
using Game;
using HarmonyLib;
using Language;
using Manager;

namespace LaunchFix.Patches;

[HarmonyPatch]
internal static class SpacecraftTooltipPatches
{
    [HarmonyPatch(typeof(SpacecraftType), nameof(SpacecraftType.GetTooltipStats))]
    [HarmonyPostfix]
    private static void GetTooltipStatsPostfix(
        SpacecraftType __instance,
        Company company,
        ref List<(string, string)> __result)
    {
        if (company == null)
            company = MonoBehaviourSingleton<GameManager>.Instance.Player;

        string format = LEManager.Get("UI.MassFormat");
        float dryMass = __instance.GetMass(company);
        string formatted = dryMass.ToPostfixString(format);
        __result.Insert(0, ("Dry Mass", formatted));
    }
}
