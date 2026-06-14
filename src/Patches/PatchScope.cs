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
}
