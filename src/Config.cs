using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Data.ScriptableObject;
using UnityEngine;

namespace LaunchFix;

internal static class ModConfig
{
    private static readonly Dictionary<string, double> _ratios = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, double> _atmPenalty = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> _features = new(StringComparer.OrdinalIgnoreCase);
    private static double _defaultRatio;

    internal static bool LvPayloadCheck => GetFeature("lvPayloadCheck");
    internal static bool SelfLaunchFuelCheck => GetFeature("selfLaunchFuelCheck");
    internal static bool SelfLaunchCost => GetFeature("selfLaunchCost");
    internal static bool SupplyMassInFuel => GetFeature("supplyMassInFuel");
    internal static bool FuelSliderDefault => GetFeature("fuelSliderDefault");
    internal static bool DetailedTooltips => GetFeature("detailedTooltips");
    internal static bool ScDryMassTooltip => GetFeature("scDryMassTooltip");
    internal static bool LvDryMass => GetFeature("lvDryMass");
    internal static bool LvGravityCost => GetFeature("lvGravityCost");
    internal static bool AtmospherePenalty => GetFeature("atmospherePenalty");
    internal static bool OrbitFuelCredit => GetFeature("orbitFuelCredit");
    internal static bool SelfLaunchDv => GetFeature("selfLaunchDv");
    internal static bool LvOrbitTransfer => GetFeature("lvOrbitTransfer");
    internal static bool FuelCalcDiagnostics => GetFeature("fuelCalcDiagnostics", false);

    private static bool GetFeature(string name, bool defaultValue = true)
    {
        return _features.TryGetValue(name, out bool enabled) ? enabled : defaultValue;
    }

    internal static void Load(string pluginDir)
    {
        string yamlPath = Path.Combine(pluginDir, "config.yaml");
        EnsureYamlExists(yamlPath);
        Parse(yamlPath);
    }

    internal static double GetLvDryMassRatio(LaunchVehicleType lvType)
    {
        if (lvType == null)
            return 0.0;

        if (_ratios.TryGetValue(lvType.ID, out double ratio))
            return Math.Max(0, ratio) / 100.0;
        return Math.Max(0, _defaultRatio) / 100.0;
    }

    internal static double GetAtmospherePenaltyFactor(LaunchVehicleType lvType)
    {
        if (lvType == null)
            return -1.0;

        if (_atmPenalty.TryGetValue(lvType.ID, out double factor))
            return factor;
        return -1.0;
    }

    private static void Parse(string yamlPath)
    {
        if (!File.Exists(yamlPath))
            return;

        _ratios.Clear();
        _atmPenalty.Clear();
        _features.Clear();
        _defaultRatio = 0;

        string section = "";
        foreach (string rawLine in File.ReadAllLines(yamlPath))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            if (line == "features:" || line == "lvDryMassRatio:" || line == "atmospherePayloadPenalty:")
            {
                section = line;
                continue;
            }

            string[] parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
                continue;

            string key = parts[0].Trim().Trim('"', '\'');
            string value = parts[1].Trim().Trim('"', '\'');
            if (key.Length == 0 || value.Length == 0)
                continue;

            if (section == "features:")
            {
                if (bool.TryParse(value, out bool enabled))
                    _features[key] = enabled;
                else
                    Debug.LogWarning($"[LaunchFix] Ignoring invalid feature toggle: {rawLine}");
            }
            else if (section == "lvDryMassRatio:")
            {
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
                {
                    Debug.LogWarning($"[LaunchFix] Ignoring invalid config entry: {rawLine}");
                    continue;
                }

                if (key.Equals("default", StringComparison.OrdinalIgnoreCase))
                    _defaultRatio = ratio;
                else
                    _ratios[key] = ratio;
            }
            else if (section == "atmospherePayloadPenalty:")
            {
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double factor))
                {
                    Debug.LogWarning($"[LaunchFix] Ignoring invalid config entry: {rawLine}");
                    continue;
                }

                _atmPenalty[key] = Math.Max(0.0, Math.Min(1.0, factor));
            }
        }

        Debug.Log($"[LaunchFix] Loaded {_ratios.Count} LV dry mass entries (default: {_defaultRatio}%), {_atmPenalty.Count} atmosphere penalty entries, {_features.Count} feature toggles from {yamlPath}");
    }

    private static void EnsureYamlExists(string yamlPath)
    {
        if (File.Exists(yamlPath))
            return;

        string directory = Path.GetDirectoryName(yamlPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(yamlPath, DefaultYaml);
    }

    private static string StripComment(string line)
    {
        int index = line.IndexOf('#');
        return index >= 0 ? line.Substring(0, index) : line;
    }

    private const string DefaultYaml =
        "# Feature toggles. Set to false to disable individual features.\n" +
        "# Omitted entries default to true (enabled).\n" +
        "features:\n" +
        "  # Include SC dry mass in LV payload checks (stock only checks cargo)\n" +
        "  lvPayloadCheck: true\n" +
        "  # Block self-launch when launch + transfer fuel exceeds SC tank capacity\n" +
        "  selfLaunchFuelCheck: true\n" +
        "  # Fix self-launch cost accounting so total fuel charged matches slider\n" +
        "  selfLaunchCost: true\n" +
        "  # Scale self-launch fuel cost with actual loaded mass (two-stage Tsiolkovsky)\n" +
        "  # Stock uses a fixed cost based on minimum transfer fuel\n" +
        "  selfLaunchDv: true\n" +
        "  # Zero out SC delta-V when LV launches to the parent body's own orbit\n" +
        "  # Stock charges the SC a phantom transfer burn for a trip the LV completes\n" +
        "  lvOrbitTransfer: true\n" +
        "  # Include supply mass in SC fuel calculations\n" +
        "  supplyMassInFuel: true\n" +
        "  # Default fuel slider to minimum required fuel\n" +
        "  fuelSliderDefault: true\n" +
        "  # Show detailed fuel breakdown in mission planner tooltips\n" +
        "  detailedTooltips: true\n" +
        "  # Show SC dry mass in spacecraft tooltips\n" +
        "  scDryMassTooltip: true\n" +
        "  # Add LV dry mass (structural weight) to launch cost\n" +
        "  lvDryMass: true\n" +
        "  # Apply surface gravity to LV launch cost once, not squared\n" +
        "  # Stock divides max payload by gravity AND multiplies cost by it\n" +
        "  lvGravityCost: true\n" +
        "  # Reduce kinetic launcher payload in atmosphere\n" +
        "  atmospherePenalty: true\n" +
        "  # Let LV-launched SCs draw fuel from orbit instead of surface\n" +
        "  orbitFuelCredit: true\n" +
        "  # Emit verbose FunctionCalculateFuel diagnostic logs\n" +
        "  fuelCalcDiagnostics: false\n" +
        "\n" +
        "# Atmosphere payload penalty for kinetic launchers.\n" +
        "# At 1.2 atm, effective payload = maxPayload * factor.\n" +
        "# At 0 atm, full payload. Linear interpolation between.\n" +
        "# Only listed LVs are affected; unlisted LVs are not penalised.\n" +
        "atmospherePayloadPenalty:\n" +
        "  id_LV_launch_spin_Fake: 0.05      # Rotary Launcher (maxPayload 100)\n" +
        "  id_LV_launch_magrails_Fake: 0.05  # Magnetic Launch Rails (maxPayload 1000)\n" +
        "\n" +
        "# LV dry mass as a percentage of costLaunch (propellant mass).\n" +
        "# dry_mass = costLaunch * (ratio / 100)\n" +
        "# LVs not listed fall back to 'default'. Set to 0 to disable.\n" +
        "lvDryMassRatio:\n" +
        "  default: 5\n" +
        "  id_Rocket_RocketType1: 5       # Sparrow (costLaunch 150, maxPayload 10)\n" +
        "  id_Rocket_RocketType2: 6       # Kestrel (costLaunch 300, maxPayload 64)\n" +
        "  id_Rocket_RocketType3: 7       # Falcon (costLaunch 400, maxPayload 42)\n" +
        "  id_Rocket_RocketType7: 5       # Hawk (costLaunch 500, maxPayload 200)\n" +
        "  id_Rocket_RocketType4: 7       # Eagle (costLaunch 3000, maxPayload 800)\n" +
        "  lv_chem_seadragon: 4           # Albatross (costLaunch 4600, maxPayload 1800)\n" +
        "  lv_chemadvanced: 8             # Condor (costLaunch 8000, maxPayload 8000)\n" +
        "  lv_nuke_small: 13              # Pelican (costLaunch 3000, maxPayload 1200)\n" +
        "  lv_nuke_mid: 12                # Magpie (costLaunch 4800, maxPayload 4000)\n" +
        "  lv_nuke_large: 10              # Teratorn (costLaunch 15000, maxPayload 20000)\n";
}
