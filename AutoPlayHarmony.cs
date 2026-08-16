using System.Reflection;
using MelonLoader;

namespace MCG_AutoPlay;

internal static class AutoPlayHarmony
{
    private static bool _applied;
    internal static bool IsTickLoopRunning { get; private set; }

    internal static bool IsApplied => _applied;

    internal static void MarkTickLoopRunning() => IsTickLoopRunning = true;

    internal static string DescribeMissingTypes() =>
        Il2CppNativeTypes.DescribeMissing(RequiredTypes);

    private static readonly (string Type, string Namespace)[] RequiredTypes =
    {
        ("MCBattleManager", ""),
        ("MCAIManager", "Battle"),
        ("MCBehaviorThreeApi", ""),
    };

    internal static bool CanResolveTypes()
    {
        if (!Il2CppNativeTypes.IsInteropReady())
            return false;

        foreach (var (type, ns) in RequiredTypes)
        {
            if (Il2CppNativeTypes.GetType(type, ns) == null)
                return false;
        }

        return true;
    }

    internal static bool Apply(HarmonyLib.Harmony harmony)
    {
        if (_applied)
            return true;

        if (!CanResolveTypes())
            return false;

        var patched = 0;
        patched += Patch(harmony, "MCBattleManager", "", "OnStartPreparePhase",
            postfix: nameof(AutoPlayPatches.MCBattleManager_OnStartPreparePhase_Postfix));
        patched += Patch(harmony, "MCBattleManager", "", "OnBeginFightCountDown",
            postfix: nameof(AutoPlayPatches.MCBattleManager_OnBeginFightCountDown_Postfix));
        patched += Patch(harmony, "MCAIManager", "Battle", "StartBattle",
            postfix: nameof(AutoPlayPatches.MCAIManager_StartBattle_Postfix));
        patched += Patch(harmony, "MCAIManager", "Battle", "EndBattle",
            postfix: nameof(AutoPlayPatches.MCAIManager_EndBattle_Postfix));
        patched += Patch(harmony, "MCBehaviorThreeApi", "", "BuyHero",
            postfix: nameof(AutoPlayPatches.MCBehaviorThreeApi_BuyHero_Postfix));
        patched += Patch(harmony, "MCBehaviorThreeApi", "", "SellHero",
            postfix: nameof(AutoPlayPatches.MCBehaviorThreeApi_SellHero_Postfix));
        patched += Patch(harmony, "MCBehaviorThreeApi", "", "RefershShop",
            postfix: nameof(AutoPlayPatches.MCBehaviorThreeApi_RefreshShop_Postfix));
        patched += Patch(harmony, "MCBehaviorThreeApi", "", "AISelectGoGoCard",
            postfix: nameof(AutoPlayPatches.MCBehaviorThreeApi_AISelectGoGoCard_Postfix));

        _applied = patched > 0;
        if (_applied)
            MelonLogger.Msg($"MCG AutoPlay patches applied ({patched}).");

        return _applied;
    }

    private static int Patch(
        HarmonyLib.Harmony harmony,
        string typeName,
        string namespaze,
        string methodName,
        string postfix)
    {
        var method = Il2CppNativeTypes.GetMethod(typeName, methodName, namespaze);
        if (method == null)
        {
            MelonLogger.Warning($"Method not found: {typeName}.{methodName}");
            return 0;
        }

        var postfixMethod = typeof(AutoPlayPatches).GetMethod(postfix, BindingFlags.Public | BindingFlags.Static);
        harmony.Patch(
            method,
            postfix: postfixMethod == null ? null : new HarmonyLib.HarmonyMethod(postfixMethod));

        MelonLogger.Msg($"Patched {typeName}.{methodName}");
        return 1;
    }
}