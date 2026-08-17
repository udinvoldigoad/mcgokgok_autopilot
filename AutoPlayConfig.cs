using MelonLoader;

namespace MCG_AutoPlay;

internal static class AutoPlayConfig
{
    private const string Category = "MCG_AutoPlay";

    internal static MelonPreferences_Category Cat = null!;
    internal static MelonPreferences_Entry<bool> Enabled = null!;
    internal static MelonPreferences_Entry<int> AIDifficulty = null!;
    internal static MelonPreferences_Entry<bool> ForceAutoDeploy = null!;
    internal static MelonPreferences_Entry<bool> VerboseWatch = null!;
    internal static MelonPreferences_Entry<int> TickMs = null!;
    internal static MelonPreferences_Entry<bool> LogGameState = null!;
    internal static MelonPreferences_Entry<bool> HumanizeActions = null!;
    internal static MelonPreferences_Entry<int> HumanMinMs = null!;
    internal static MelonPreferences_Entry<int> HumanMaxMs = null!;

    internal static void Init()
    {
        Cat = MelonPreferences.CreateCategory(Category, "MCG AutoPlay (Tier 3)");
        Enabled = Cat.CreateEntry("Enabled", true, "Enable full autopilot during battles");
        AIDifficulty = Cat.CreateEntry("AIDifficulty", 101, "Built-in MCAI difficulty (101=default, up to 1006)");
        ForceAutoDeploy = Cat.CreateEntry("ForceAutoDeploy", true, "Force auto board placement each prepare phase");
        VerboseWatch = Cat.CreateEntry("VerboseWatch", true, "Log shop/deploy/GoGo actions to MelonLoader console");
        TickMs = Cat.CreateEntry("TickMs", 150, "AI tick interval in milliseconds");
        LogGameState = Cat.CreateEntry("LogGameState", false, "OBSERVE: log parsed GameState each prepare phase (no actions)");
        HumanizeActions = Cat.CreateEntry("HumanizeActions", true, "Pace AI actions at human-like random intervals instead of instant bursts");
        HumanMinMs = Cat.CreateEntry("HumanMinMs", 900, "Minimum random delay between AI action passes (ms)");
        HumanMaxMs = Cat.CreateEntry("HumanMaxMs", 2200, "Maximum random delay between AI action passes (ms)");
    }
}