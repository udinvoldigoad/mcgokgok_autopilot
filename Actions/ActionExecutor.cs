namespace MCG_AutoPlay.Actions;

/// <summary>
/// Menerjemahkan Decision menjadi panggilan ke Game API via BattleBridgeHelper.
/// (PRD Phase 6: Decision -> ActionExecutor -> Game API).
///
/// Aksi nyata memakai string member/method yang sama dengan bot lama (terbukti
/// bekerja). Jika API belum siap saat dieksekusi, aksi di-skip dengan aman.
/// </summary>
internal static class ActionExecutor
{
    /// <summary>Eksekusi satu keputusan. Kembalikan true bila berhasil diteruskan.</summary>
    internal static bool Execute(Decision decision)
    {
        switch (decision.Kind)
        {
            case DecisionKind.Buy:
                return Buy(decision.Slot);
            case DecisionKind.Sell:
                return Sell(decision.Slot);
            case DecisionKind.RefreshShop:
                return RefreshShop();
            case DecisionKind.LevelUp:
                return LevelUp();
            case DecisionKind.LockShop:
                return LockShop();
            default:
                return false;
        }
    }

    private static bool Buy(int slot) => BattleBridgeHelper.InvokeShopSelect(slot);
    private static bool Sell(int slot) => BattleBridgeHelper.InvokeSell(slot);
    private static bool RefreshShop() => BattleBridgeHelper.InvokeRefreshShop();
    private static bool LevelUp() => BattleBridgeHelper.InvokeLevelUp();
    private static bool LockShop() => BattleBridgeHelper.InvokeLockShop();
}
