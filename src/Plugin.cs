using BepInEx;
using HarmonyLib;

namespace LaunchFix;

[BepInPlugin("com.launchfix", "Launch Fix", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, "com.launchfix");
    }
}
