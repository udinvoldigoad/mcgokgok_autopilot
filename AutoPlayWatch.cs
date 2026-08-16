using MelonLoader;

namespace MCG_AutoPlay;

internal static class AutoPlayWatch
{
    internal static void Log(string message)
    {
        if (!AutoPlayConfig.VerboseWatch.Value)
            return;

        MelonLogger.Msg($"[WATCH] {message}");
    }

    internal static void Action(string action) => Log(action);

    internal static void Status(string status) => MelonLogger.Msg($"[AUTOPILOT] {status}");
}