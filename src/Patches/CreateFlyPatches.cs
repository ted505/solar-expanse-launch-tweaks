using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFix.Patches;

/// <summary>
/// When a mission is confirmed, CreateFly sources cargo-tab resources
/// from StartHermesCase.  Our OrbitFuelCredit promotion moves
/// StartHermesCase to orbit, but cargo-tab fuel is physically on the
/// surface — sourcing from orbit finds nothing and zeroes the cargo.
///
/// Suppress the promotion during CreateFly so cargo is sourced from
/// the surface.  Stock Hermes cases (orbit SCs) are unaffected because
/// the promotion postfix already skips when stock result != Start.
/// </summary>
[HarmonyPatch(typeof(PMTabSchedule), "CreateFly")]
internal static class CreateFlyPatches
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        if (!ModConfig.OrbitFuelCredit) return;
        OrbitFuelPatches.SuppressPromotion = true;
    }

    [HarmonyFinalizer]
    private static void Finalizer()
    {
        OrbitFuelPatches.SuppressPromotion = false;
    }
}
