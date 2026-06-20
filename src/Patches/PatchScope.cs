using Game;
using Game.UI.Windows.Elements.PlanMissionElements;
using Manager;

namespace LaunchFix.Patches;

internal static class PatchScope
{
    internal static bool IsAICompany(Company company)
    {
        var manager = MonoBehaviourSingleton<GameManager>.Instance;
        if (company == null || manager == null)
            return false;
        return company != manager.Player;
    }

    internal static bool IsAIMission(PMMissionParameter p)
    {
        return p != null && IsAICompany(p.FlyCompany);
    }

    /// <summary>
    /// Asteroid-pulling and interstellar missions use a different payload mass
    /// basis, so the LV payload check must not apply to them.
    /// </summary>
    internal static bool IsAsteroidOrInterstellar(PMMissionParameter p)
    {
        if (p == null)
            return false;

        var cargo = p.CargoAll;
        if (cargo != null && cargo.entireAsteroid)
            return true;

        var sc = p.SC;
        return sc != null
            && sc.GetTypeSpaceCraft().IsInterstellarShipOrAsteroidPullingShipFromFacility;
    }
}
