namespace MCG_AutoPlay;

internal static class AutoPlayPatches
{
    public static void MCBattleManager_OnStartPreparePhase_Postfix()
    {
        Safe.Run(() => AutoPlayController.OnPreparePhase(), "OnStartPreparePhase");
    }

    public static void MCBattleManager_OnBeginFightCountDown_Postfix()
    {
        Safe.Run(() => AutoPlayController.OnFightCountdown(), "OnBeginFightCountDown");
    }

    public static void MCAIManager_StartBattle_Postfix()
    {
        Safe.Run(() => AutoPlayController.OnBattleStart(), "StartBattle");
    }

    public static void MCAIManager_EndBattle_Postfix()
    {
        Safe.Run(() => AutoPlayController.OnBattleEnd(), "EndBattle");
    }

    public static void MCBehaviorThreeApi_BuyHero_Postfix(object __instance, bool __result, int slot)
    {
        Safe.Run(() =>
        {
            if (!AutoPlayController.ShouldLogApi(__instance))
                return;

            AutoPlayController.LogApiAction("BUY", slot, __result);
        }, "BuyHero");
    }

    public static void MCBehaviorThreeApi_SellHero_Postfix(object __instance, bool __result, int slot)
    {
        Safe.Run(() =>
        {
            if (!AutoPlayController.ShouldLogApi(__instance))
                return;

            AutoPlayController.LogApiAction("SELL", slot, __result);
        }, "SellHero");
    }

    public static void MCBehaviorThreeApi_RefreshShop_Postfix(object __instance, int __result)
    {
        Safe.Run(() =>
        {
            if (__result == 0 || !AutoPlayController.ShouldLogApi(__instance))
                return;

            AutoPlayWatch.Action($"REFRESH cost={__result} round={AutoPlayController.GetRoundLabel()}");
        }, "RefershShop");
    }

    public static void MCBehaviorThreeApi_AISelectGoGoCard_Postfix(object __instance, bool __result)
    {
        Safe.Run(() =>
        {
            if (!AutoPlayController.ShouldLogApi(__instance))
                return;

            AutoPlayController.LogApiAction("GOGO_CARD", __result);
        }, "AISelectGoGoCard");
    }

    private static class Safe
    {
        internal static void Run(Action action, string hook)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[MCG AutoPlay] {hook}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
