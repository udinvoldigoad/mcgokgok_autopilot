using System.Collections;
using MelonLoader;

[assembly: MelonInfo(typeof(MCG_AutoPlay.AutoPlayMod), "MCG AutoPlay", "1.0.4", "MCG")]
[assembly: MelonGame("mulonggame", "MagicChessGoGo")]

namespace MCG_AutoPlay;

public sealed class AutoPlayMod : MelonMod
{
    private static HarmonyLib.Harmony? _harmony;
    private const int MaxPatchAttempts = 120;

    public override void OnInitializeMelon()
    {
        AutoPlayConfig.Init();
        MelonLogger.Msg("MCG AutoPlay Tier 3 loaded. Configure via UserData/MelonPreferences.cfg [MCG_AutoPlay].");
        MelonLogger.Msg("Watch mode: VerboseWatch=true logs all shop/deploy picks to this console.");
    }

    public override void OnLateInitializeMelon()
    {
        _harmony = new HarmonyLib.Harmony("MCG.AutoPlay");
        MelonCoroutines.Start(PatchRetryLoop());
        MelonCoroutines.Start(TickLoop());
    }

    private static IEnumerator PatchRetryLoop()
    {
        var wait = new UnityEngine.WaitForSeconds(0.5f);

        for (var attempt = 1; attempt <= MaxPatchAttempts; attempt++)
        {
            if (_harmony != null && AutoPlayHarmony.Apply(_harmony))
                yield break;

            if (attempt == 1 || attempt % 20 == 0)
                MelonLogger.Msg($"Waiting for Il2Cpp interop types (attempt {attempt}/{MaxPatchAttempts})...");

            yield return wait;
        }

        if (!AutoPlayHarmony.IsApplied)
        {
            MelonLogger.Warning(
                $"MCG AutoPlay initial patch window ended. Missing: {AutoPlayHarmony.DescribeMissingTypes()} — background retry continues.");
        }
    }

    private static IEnumerator TickLoop()
    {
        AutoPlayHarmony.MarkTickLoopRunning();

        while (true)
        {
            if (!AutoPlayHarmony.IsApplied && _harmony != null)
                AutoPlayHarmony.Apply(_harmony);

            if (AutoPlayHarmony.IsApplied)
            {
                var ms = Math.Clamp(AutoPlayConfig.TickMs.Value, 50, 2000);
                try
                {
                    AutoPlayController.Tick(ms);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Autopilot tick skipped: {ex.GetType().Name}: {ex.Message}");
                }

                yield return new UnityEngine.WaitForSeconds(ms / 1000f);
            }
            else
            {
                yield return new UnityEngine.WaitForSeconds(2f);
            }
        }
    }
}
